#include "stdafx.h"
#include <stdio.h>

using namespace System;

extern "C"
{
    //__declspec(dllimport)
	void Initialize();
	void Connect();
	void InitCharacterRecognition();
	void TestText();
	void Zoom();
	int GetX();
	int GetY();
}

int main(array<System::String ^> ^args)
{
	Initialize();
	Connect();
	int x = GetX();
	int y = GetY();
	//Test2();
	InitCharacterRecognition();
	//TestText();
	Zoom();
	getchar();
    return 0;
}
