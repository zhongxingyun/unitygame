#ifndef _SYSTEM_H
#define _SYSTEM_H

#include "Singleton.hxx"
#include "SystemEndian.hxx"

#include "Platform.hxx"
BEGINNAMESPACE(NSExcelExport)

class System : public Singleton < System >
{
protected:
	SysEndian m_sysEndian;				// ����ϵͳ��С��
public:
	void checkEndian();		// ��Ȿ��ϵͳ�Ǵ�˻���С��
	bool isEndianDiffFromSys(SysEndian rhv);	// �����ϵͳ�Ĵ��С���Ƿ���ͬ
};

ENDNAMESPACE(NSExcelExport)

#endif