#include "stdafx.h"
#include <stdio.h>

using namespace System;

extern "C"
{
    //__declspec(dllexport)
	void Initialize();
	void Connect();
	int GetX();
	int GetY();
	const char * Test2();
}

int main(array<System::String ^> ^args)
{
	Initialize();
	Connect();
	int x = GetX();
	int y = GetY();
	Test2();
	getchar();
    return 0;
}
