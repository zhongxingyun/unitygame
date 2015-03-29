﻿using SDK.Lib;
using System;
using System.Collections.Generic;
using System.Text;

namespace SDK.Common
{
    /**
     *@brief ByteBuffer 功能
     */
    public class ByteBuffer
    {
        public byte[] m_intByte = new byte[sizeof(int)];
        public byte[] m_shortByte = new byte[sizeof(short)];
        public byte[] m_longByte = new byte[sizeof(long)];

        protected DynamicBuffer m_dynBuff;
        protected uint m_position;          // 当前可以读取的位置索引
        protected Endian m_endian;          // 大端小端

        protected byte[] m_padBytes;

        public ByteBuffer(uint initCapacity = DynamicBuffer.INIT_CAPACITY, uint maxCapacity = DynamicBuffer.MAX_CAPACITY, Endian endian = Endian.LITTLE_ENDIAN)
        {
            m_endian = endian;        // 缓冲区默认是小端的数据，因为服务器是 linux 的
            m_dynBuff = new DynamicBuffer(initCapacity, maxCapacity);
        }

        public DynamicBuffer dynBuff
        {
            get
            {
                return m_dynBuff;
            }
        }

		public uint bytesAvailable
        {
            get
            {
                return (m_dynBuff.size - m_position);
            }
        }

		public Endian endian
        {
            get
            {
                return m_endian;
            }
            set
            {
                m_endian = value;
            }
        }

        public void setEndian(Endian end)
        {
            m_endian = end;
        }

		public uint length
        {
            get
            {
                return m_dynBuff.size;
            }
            set
            {
                m_dynBuff.size = value;
            }
        }

        public void setPos(uint pos)
        {
            m_position = pos;
        }

        public uint getPos()
        {
            return m_position;
        }

		public uint position
        {
            get
            {
                return m_position;
            }
            set
            {
                m_position = value;
            }
        }

		public void clear ()
        {
            m_position = 0;
            m_dynBuff.size = 0;
        }

        // 检查是否有足够的大小可以扩展
        protected bool canWrite(uint delta)
        {
            if(m_dynBuff.size + delta > m_dynBuff.capacity)
            {
                return false;
            }

            return true;
        }

        // 读取检查
        protected bool canRead(uint delta)
        {
            if (m_position + delta > m_dynBuff.size)
            {
                return false;
            }

            return true;
        }

        protected void extendDeltaCapicity(uint delta)
        {
            m_dynBuff.extendDeltaCapicity(delta);
        }

        protected void advPos(uint num)
        {
            m_position += num;
        }

        protected void advPosAndLen(uint num)
        {
            m_position += num;
            length = m_position;
        }

        // 压缩
        public uint compress(uint len_ = 0, CompressionAlgorithm algorithm = CompressionAlgorithm.ZLIB)
        {
            len_ = (len_ == 0 ? length : len_);

            byte[] retByte = null;
            uint retSize = 0;
            Compress.CompressData(m_dynBuff.buff, position, len_, ref retByte, ref retSize, algorithm);

            replace(retByte, 0, retSize, position, len_);

            return retSize;
        }

        // 解压
        public uint uncompress(uint len_ = 0, CompressionAlgorithm algorithm = CompressionAlgorithm.ZLIB)
        {
            len_ = (len_ == 0 ? length : len_);

            byte[] retByte = null;
            uint retSize = 0;
            Compress.DecompressData(m_dynBuff.buff, position, length, ref retByte, ref retSize, algorithm);

            replace(retByte, 0, retSize, position, len_);

            return retSize;
        }

        // 加密，使用 des 对称数字加密算法，加密8字节补齐，可能会导致变长
        public uint encrypt(CryptContext cryptContext, uint len_ = 0)
        {
#if OBSOLETE
            len_ = (len_ == 0 ? length : len_);

            byte[] retByte = null;
            // 只有 8 个字节的时候才加密
            uint leftCnt = len_ % 8;  // 剩余的数量
            uint cryptCnt = leftCnt;

            if (len_ >= 8)
            {
                Crypt.encryptData(m_dynBuff.buff, position, len_ - leftCnt, ref retByte, cryptKey);
                writeBytes(retByte, 0, (uint)retByte.Length, false);
                cryptCnt += (uint)retByte.Length;
            }

            if (leftCnt > 0) // 如果还有剩余的字节没有加密，还需要增加长度
            {
                position += leftCnt;
            }

            return cryptCnt;
#endif
            len_ = (len_ == 0 ? length : len_);
            uint alignLen_ = ((len_ + 7) / 8) * 8; // 补齐 8 个字节，因为加密是 8 个字节一次加密，只要是 8 个字节的整数倍，无论多少个都可以任意解压
            uint leftLen_ = alignLen_ - len_;
            if(leftLen_ > 0)
            {
                if(m_padBytes == null)
                {
                    m_padBytes = new byte[8];
                }

                // 保存数据，然后补 0
                Array.Copy(m_dynBuff.buff, position + len_, m_padBytes, 0, leftLen_);
                Array.Clear(m_dynBuff.buff, (int)(position + len_), (int)leftLen_);
            }

            if (len_ == 0)      // 修正之后还等于 0 
            {
                return 0;
            }

            if (alignLen_ > m_dynBuff.capacity)   // 如果最后加密(由于补齐)的长度大于原始长度
            {
                length = alignLen_;
            }

            byte[] retByte = null;

            Crypt.encryptData(m_dynBuff.buff, position, alignLen_, ref retByte, cryptContext);  // 注意补齐不一定是 0 
            Array.Copy(m_padBytes, 0, m_dynBuff.buff, position + len_, leftLen_);       // 拷贝回去
            replace(retByte, 0, alignLen_, position, len_);

            return alignLen_;
        }

        // 解密，现在必须 8 字节对齐解密
        public void decrypt(CryptContext cryptContext, uint len_ = 0)
        {
            len_ = (len_ == 0 ? length : len_);

            byte[] retByte = null;

            if (0 == len_)
            {
                return;
            }

            Crypt.decryptData(m_dynBuff.buff, position, len_, ref retByte, cryptContext);
            writeBytes(retByte, 0, (uint)retByte.Length, false);
        }

        public ByteBuffer readBoolean(ref bool tmpBool)
        {
            if (canRead(sizeof(bool)))
            {
                tmpBool = System.BitConverter.ToBoolean(m_dynBuff.buff, (int)m_position);
                advPos(sizeof(bool));
            }

            return this;
        }

        public ByteBuffer readInt8(ref byte tmpByte)
        {
            if (canRead(sizeof(char)))
            {
                tmpByte = (byte)System.BitConverter.ToChar(m_dynBuff.buff, (int)m_position);
                advPos(sizeof(char));
            }

            return this;
        }

        public ByteBuffer readUnsignedInt8(ref byte tmpByte)
        {
            if (canRead(sizeof(byte)))
            {
                tmpByte = (byte)System.BitConverter.ToChar(m_dynBuff.buff, (int)m_position);
                advPos(sizeof(byte));
            }

            return this;
        }

        public ByteBuffer readInt16(ref short tmpShort)
        {
            if (canRead(sizeof(short)))
            {
                if (m_endian == SystemEndian.m_sEndian)
                {
                    tmpShort = System.BitConverter.ToInt16(m_dynBuff.buff, (int)m_position);
                }
                else
                {
                    Array.Copy(m_dynBuff.buff, (int)m_position, m_shortByte, 0, sizeof(short));
                    Array.Reverse(m_shortByte);
                    tmpShort = System.BitConverter.ToInt16(m_shortByte, 0);
                }

                advPos(sizeof(short));
            }

            return this;
        }

        public ByteBuffer readUnsignedInt16(ref ushort tmpUshort)
        {
            if (canRead(sizeof(ushort)))
            {
                if (m_endian == SystemEndian.m_sEndian)
                {
                    tmpUshort = System.BitConverter.ToUInt16(m_dynBuff.buff, (int)m_position);
                }
                else
                {
                    Array.Copy(m_dynBuff.buff, (int)m_position, m_shortByte, 0, sizeof(ushort));
                    Array.Reverse(m_shortByte);
                    tmpUshort = System.BitConverter.ToUInt16(m_shortByte, 0);
                }

                advPos(sizeof(ushort));
            }

            return this;
        }

        public ByteBuffer readInt32(ref int tmpInt)
        {
            if (canRead(sizeof(int)))
            {
                if (m_endian == SystemEndian.m_sEndian)
                {
                    tmpInt = System.BitConverter.ToInt32(m_dynBuff.buff, (int)m_position);
                }
                else
                {
                    Array.Copy(m_dynBuff.buff, (int)m_position, m_intByte, 0, sizeof(int));
                    Array.Reverse(m_intByte);
                    tmpInt = System.BitConverter.ToInt32(m_intByte, 0);
                }
                advPos(sizeof(int));
            }

            return this;
        }

        public ByteBuffer readFloat(ref float tmpFloat)
        {
            if (canRead(sizeof(float)))
            {
                if (m_endian == SystemEndian.m_sEndian)
                {
                    tmpFloat = System.BitConverter.ToInt32(m_dynBuff.buff, (int)m_position);
                }
                else
                {
                    Array.Copy(m_dynBuff.buff, (int)m_position, m_intByte, 0, sizeof(float));
                    Array.Reverse(m_intByte);
                    tmpFloat = System.BitConverter.ToInt32(m_intByte, 0);
                }
                advPos(sizeof(float));
            }

            return this;
        }

        public ByteBuffer readDouble(ref double tmpDouble)
        {
            if (canRead(sizeof(double)))
            {
                if (m_endian == SystemEndian.m_sEndian)
                {
                    tmpDouble = System.BitConverter.ToInt32(m_dynBuff.buff, (int)m_position);
                }
                else
                {
                    Array.Copy(m_dynBuff.buff, (int)m_position, m_longByte, 0, sizeof(double));
                    Array.Reverse(m_longByte);
                    tmpDouble = System.BitConverter.ToInt32(m_longByte, 0);
                }
                advPos(sizeof(double));
            }

            return this;
        }

        public ByteBuffer readUnsignedInt32(ref uint tmpUint)
        {
            if (canRead(sizeof(uint)))
            {
                if (m_endian == SystemEndian.m_sEndian)
                {
                    tmpUint = System.BitConverter.ToUInt32(m_dynBuff.buff, (int)m_position);
                }
                else
                {
                    Array.Copy(m_dynBuff.buff, (int)m_position, m_intByte, 0, sizeof(uint));
                    Array.Reverse(m_intByte);
                    tmpUint = System.BitConverter.ToUInt32(m_intByte, 0);
                }

                advPos(sizeof(uint));
            }

            return this;
        }

        public ByteBuffer readUnsignedLong(ref ulong tmpUlong)
        {
            if (canRead(sizeof(ulong)))
            {
                if (m_endian == SystemEndian.m_sEndian)
                {
                    tmpUlong = System.BitConverter.ToUInt64(m_dynBuff.buff, (int)m_position);
                }
                else
                {
                    Array.Copy(m_dynBuff.buff, (int)m_position, m_longByte, 0, sizeof(ulong));
                    Array.Reverse(m_longByte);
                    tmpUlong = System.BitConverter.ToUInt64(m_longByte, 0);
                }

                advPos(sizeof(ulong));
            }

            return this;
        }

        public ByteBuffer readMultiByte(ref string tmpStr, uint len, Encoding charSet)
        {
            // http://blog.sina.com.cn/s/blog_6e51df7f0100tj9z.html
            // gbk和utf-8都是以单个字节表示数字的，所以不存在字节序问题，在多个不同系统架构都用。对于utf-16，则是以双字节表示一个整数，所以为会有字节序问题，分大小端unicode
            // http://msdn.microsoft.com/zh-cn/library/system.text.encoding.bigendianunicode%28v=vs.80%29.aspx
            // Encoding  u7    = Encoding.UTF7;
            // Encoding  u8    = Encoding.UTF8;
            // Encoding  u16LE = Encoding.Unicode;
            // Encoding  u16BE = Encoding.BigEndianUnicode;
            // Encoding  u32   = Encoding.UTF32;
            // 如果是 unicode ，需要大小端判断
            if (canRead(len))
            {
                tmpStr = charSet.GetString(m_dynBuff.buff, (int)m_position, (int)len);
                advPos(len);
            }

            return this;
        }

        // 这个是字节读取，没有大小端的区别
        public ByteBuffer readBytes(ref byte[] tmpBytes, uint len)
        {
            if (canRead(len))
            {
                Array.Copy(m_dynBuff.buff, (int)m_position, tmpBytes, 0, (int)len);
                advPos(len);
            }

            return this;
        }

        public void writeUnsignedInt8(byte value)
        {
            if (!canWrite(sizeof(byte)))
            {
                extendDeltaCapicity(sizeof(byte));
            }
            m_dynBuff.buff[m_position] = value;
            advPosAndLen(sizeof(byte));
        }

		public void writeInt16 (short value)
        {
            if (!canWrite(sizeof(short)))
            {
                extendDeltaCapicity(sizeof(short));
            }

            m_shortByte = System.BitConverter.GetBytes(value);
            if (m_endian != SystemEndian.m_sEndian)
            {
                Array.Reverse(m_shortByte);
            }
            Array.Copy(m_shortByte, 0, m_dynBuff.buff, m_position, sizeof(short));

            advPosAndLen(sizeof(short));
        }

        public void writeUnsignedInt16(ushort value)
        {
            if (!canWrite(sizeof(ushort)))
            {
                extendDeltaCapicity(sizeof(ushort));
            }

            m_shortByte = System.BitConverter.GetBytes(value);
            if (m_endian != SystemEndian.m_sEndian)
            {
                Array.Reverse(m_shortByte);
            }
            Array.Copy(m_shortByte, 0, m_dynBuff.buff, m_position, sizeof(ushort));

            advPosAndLen(sizeof(ushort));
        }

        public void writeInt32(int value)
        {
            if (!canWrite(sizeof(int)))
            {
                extendDeltaCapicity(sizeof(int));
            }

            m_intByte = System.BitConverter.GetBytes(value);
            if (m_endian != SystemEndian.m_sEndian)
            {
                Array.Reverse(m_intByte);
            }
            Array.Copy(m_intByte, 0, m_dynBuff.buff, m_position, sizeof(int));

            advPosAndLen(sizeof(int));
        }

		public void writeUnsignedInt32 (uint value, bool bchangeLen = true)
        {
            if (!canWrite(sizeof(uint)))
            {
                extendDeltaCapicity(sizeof(uint));
            }

            m_intByte = System.BitConverter.GetBytes(value);
            if (m_endian != SystemEndian.m_sEndian)
            {
                Array.Reverse(m_intByte);
            }
            Array.Copy(m_intByte, 0, m_dynBuff.buff, m_position, sizeof(uint));

            if (bchangeLen)
            {
                advPosAndLen(sizeof(uint));
            }
            else
            {
                advPos(sizeof(uint));
            }
        }

        public void writeUnsignedLong(ulong value)
        {
            if (!canWrite(sizeof(long)))
            {
                extendDeltaCapicity(sizeof(long));
            }

            m_longByte = System.BitConverter.GetBytes(value);
            if (m_endian != SystemEndian.m_sEndian)
            {
                Array.Reverse(m_longByte);
            }
            Array.Copy(m_longByte, 0, m_dynBuff.buff, m_position, sizeof(long));

            advPosAndLen(sizeof(long));
        }

        // 写入字节， bchangeLen 是否改变长度
        public void writeBytes(byte[] value, uint start, uint len, bool bchangeLen = true)
        {
            if (len > 0)            // 如果有长度才写入
            {
                if (!canWrite(len))
                {
                    extendDeltaCapicity(len);
                }
                Array.Copy(value, start, m_dynBuff.buff, m_position, len);
                if (bchangeLen)
                {
                    advPosAndLen(len);
                }
                else
                {
                    advPos(len);
                }
            }
        }

        // 写入字符串
        public void writeMultiByte(string value, Encoding charSet, int len)
        {
            int num = 0;

            if (null != value)
            {
                //char[] charPtr = value.ToCharArray();
                num = charSet.GetByteCount(value);

                if (0 == len)
                {
                    len = num;
                }

                if (!canWrite((uint)len))
                {
                    extendDeltaCapicity((uint)len);
                }

                if (num <= len)
                {
                    Array.Copy(charSet.GetBytes(value), 0, m_dynBuff.buff, m_position, num);
                }
                else
                {
                    Array.Copy(charSet.GetBytes(value), 0, m_dynBuff.buff, m_position, len);
                }
                advPosAndLen((uint)len);
            }
            else
            {
                if (!canWrite((uint)len))
                {
                    extendDeltaCapicity((uint)len);
                }

                advPosAndLen((uint)len);
            }
        }

        // 替换已经有的一段数据
        protected void replace(byte[] srcBytes, uint srcStartPos = 0, uint srclen_ = 0, uint destStartPos = 0, uint destlen_ = 0)
        {
            uint curPos = position;     // 保存当前位置

            uint lastLeft = length - destStartPos - destlen_;        // 最后一段的长度
            length = destStartPos + srclen_ + lastLeft;      // 设置大小，保证足够大小空间

            position = destStartPos + srclen_;
            if (lastLeft > 0)
            {
                writeBytes(m_dynBuff.buff, destStartPos + destlen_, lastLeft, false);          // 这个地方自己区域覆盖自己区域，可以保证自己不覆盖自己区域
            }

            position = destStartPos;
            writeBytes(srcBytes, srcStartPos, srclen_, false);
        }

        public void insertUnsignedInt32(uint value)
        {
            length += sizeof(int);       // 扩大长度
            writeUnsignedInt32(value);     // 写入
        }

        public ByteBuffer readUnsignedLongByOffset(ref ulong tmpUlong, uint offset)
        {
            position = offset;
            readUnsignedLong(ref tmpUlong);

            return this;
        }
    }
}