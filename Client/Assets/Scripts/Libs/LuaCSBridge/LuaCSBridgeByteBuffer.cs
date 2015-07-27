﻿using LuaInterface;
using SDK.Common;
using System;

namespace SDK.Lib
{
    /**
     * @brief ByteBuffer 和 Lua 沟通
     */
    public class LuaCSBridgeByteBuffer : LuaCSBridge
    {
        public const string CLEAR = "clearFromCS";
        public const string WRITEINT8 = "writeInt8FromCS";

        protected LuaTable m_luaTable;      // LuaTable

        public LuaCSBridgeByteBuffer() : 
            base ("NetMsgData")
            //base("ByteBuffer")
        {
            string path = "LuaScript/DataStruct/NetMsgData.lua";
            Ctx.m_instance.m_luaMgr.DoFile(path);
            m_luaTable = Ctx.m_instance.m_luaMgr.GetLuaTable(m_tableName);
        }

        // 更新 Lua 中表的数据
        public void updateLuaByteBuffer(ByteBuffer ba)
        {
            CallClassMethod(LuaCSBridgeByteBuffer.CLEAR);       // 清除字节缓冲区
            for(int idx = 0; idx < ba.dynBuff.size; ++idx)
            {
                //m_luaTable[idx] = ba.dynBuff.buff[idx];               // 这样是直接加入表中
                //CallClassMethod("writeInt8", ba.dynBuff.buff[idx]);         // 写入每一个字节到缓冲区中，直接传递数字类型调用函数，这个数字会被作为 UserData ，如果传递数字，需要传递字符串才行
                //object ret = CallClassMethod("writeInt8", ba.dynBuff.buff[idx].ToString());
                //int aaa = 10;
                writeInt8ToLua(ba.dynBuff.buff[idx]);
            }
        }

        // writeInt8 函数调用，写一个字节到 Lua 表中
        protected void writeInt8ToLua(int oneByte)
        {
            //string funcName = "writeInt8";
            string fullFuncName = "";               // 完全的有表的完全名字
            if (!String.IsNullOrEmpty(m_tableName))  // 如果在 _G 表中
            {
                fullFuncName = m_tableName + "." + WRITEINT8;
                LuaTable luaTable = Ctx.m_instance.m_luaMgr.GetLuaTable(m_tableName);

                IntPtr L = Ctx.m_instance.m_luaMgr.lua.L;
                int oldTop = LuaDLL.lua_gettop(L);

                // 获取表
                LuaDLL.lua_pushstring(L, m_tableName);
                LuaDLL.lua_rawget(L, LuaIndexes.LUA_GLOBALSINDEX);      // 从 _G 表中获取数据
                // 检查类型
                LuaTypes type = LuaDLL.lua_type(L, -1);
                if (type != LuaTypes.LUA_TTABLE)
                {
                    LuaDLL.lua_settop(L, oldTop);
                    return;
                }
                // 获取函数
                LuaDLL.lua_pushstring(L, WRITEINT8);
                LuaDLL.lua_rawget(L, -2);
                type = LuaDLL.lua_type(L, -1);
                if (type != LuaTypes.LUA_TFUNCTION)
                {
                    LuaDLL.lua_settop(L, oldTop);
                    return;
                }
                // 放 Lua 表
                luaTable.push(L);
                type = LuaDLL.lua_type(L, -1);
                if (type != LuaTypes.LUA_TTABLE)
                {
                    LuaDLL.lua_settop(L, oldTop);
                    return;
                }
                // 放数字
                LuaDLL.lua_pushinteger(L, oneByte);
                type = LuaDLL.lua_type(L, -1);
                if (type != LuaTypes.LUA_TNUMBER)
                {
                    LuaDLL.lua_settop(L, oldTop);
                    return;
                }

                int nArgs = 0;
                nArgs = 2;
                int error = LuaDLL.lua_pcall(L, nArgs, -1, -nArgs - 2);
                if (error != 0)
                {
                    string err = LuaDLL.lua_tostring(L, -1);
                    LuaDLL.lua_settop(L, oldTop);
                    return;
                }

                LuaDLL.lua_settop(L, oldTop);
            }
        }
    }
}