#include "stdafx.h"
#include <windows.h>
#include <string>
#include <sstream>
#include "EyeTrackingCLR.h"

using namespace System; // Object
using namespace System::IO; // Path
using namespace System::Reflection; // Assembly
using namespace System::Windows::Forms;
using namespace System::Windows::Input;

static String^ GetLocalAssemblyPath(String^ name)
{
	// find other dlls in the same folder as the one
	return Path::Combine(Path::GetDirectoryName(Assembly::GetExecutingAssembly()->Location), name);
}

/*
Because we are loading this dll from a Python script instead of a .net application
the dependencies will not load automatically. We need to help it by registering
an assembly resolve. This will trigger the first time a function is called
that requires the dll, before the control enters that function.
*/
static Assembly^ AssemblyResolve(Object^ Sender, ResolveEventArgs^ args)
{
    AssemblyName^ assemblyName = gcnew AssemblyName(args->Name);
 
	//MessageBox::Show("DLL: " + assemblyName->Name);

	if (assemblyName->Name == "EyeTracking" ||
		assemblyName->Name == "Tobii.Interaction.Net" ||
		assemblyName->Name == "Tobii.Interaction.Model" ||
		assemblyName->Name == "Tobii.EyeX.Client")
    {
		String^ assemblyPath = GetLocalAssemblyPath(assemblyName->Name + ".dll");
		//MessageBox::Show("Assembly path: " + assemblyPath);
        try
		{
			return Assembly::LoadFile(assemblyPath);
		}
		catch (Exception^ e)
		{
			MessageBox::Show("Exception:\n" + e->Message + "\nBacktrace:\n" + e->StackTrace);
		}
    }

    return nullptr;
}

static void GetFocusedText()
{
	MessageBox::Show("GetFocusedText");
}

extern "C"
{
	bool isInitialized = false;
	bool followGaze = false;
	char *s_text = nullptr;
	std::string s_string = "";
     
    __declspec(dllexport)
	void Initialize()
    {
		//MessageBox::Show("initialize");

        if (!isInitialized)
        {
            AppDomain::CurrentDomain->AssemblyResolve += gcnew ResolveEventHandler(AssemblyResolve);
             
            isInitialized = true;
        }
    }

	__declspec(dllexport)
	void Connect()
	{
		return EyeTrackingHooks::EyeTracking::Connect();
	}

	__declspec(dllexport)
	int Test()
	{
		return EyeTrackingHooks::EyeTracking::Test();
	}

	__declspec(dllexport)
	int GetX()
	{
		return EyeTrackingHooks::EyeTracking::GetX();
	}

	__declspec(dllexport)
	int GetY()
	{
		return EyeTrackingHooks::EyeTracking::GetY();
	}

	__declspec(dllexport)
	void EnableFollowGaze()
	{
		return EyeTrackingHooks::EyeTracking::EnableFollowGaze();
	}

	__declspec(dllexport)
	void DisableFollowGaze()
	{
		return EyeTrackingHooks::EyeTracking::DisableFollowGaze();
	}

	__declspec(dllexport)
	void Zoom()
	{
		return EyeTrackingHooks::EyeTracking::Zoom();
	}

	__declspec(dllexport)
	void Unzoom()
	{
		return EyeTrackingHooks::EyeTracking::Unzoom();
	}

	__declspec(dllexport)
	void ZoomPush()
	{
		return EyeTrackingHooks::EyeTracking::ZoomPush();
	}

	__declspec(dllexport)
	void TeleportCursor()
	{
		return EyeTrackingHooks::EyeTracking::TeleportCursor();
	}
	
	__declspec(dllexport)
	void Strafe()
	{
		return EyeTrackingHooks::EyeTracking::Strafe();
	}

	__declspec(dllexport)
	void StopMoving()
	{
		return EyeTrackingHooks::EyeTracking::StopMoving();
	}

	__declspec(dllexport)
	const char * Test2()
	{
		System::Windows::IInputElement^ input = Keyboard::FocusedElement;
		if (!input)
		{
			s_text = "No focus";
			return s_text;
		}
		
		TextBox^ i = dynamic_cast<TextBox^>(input);
		if (i)
		{
			s_text = "TextBox";
			return s_text;
		}
		else
		{
			s_text = "Not TextBox";
			return s_text;
		}

		HWND hwndForeground = GetForegroundWindow();
		if (hwndForeground == NULL)
		{
			s_text = "Foreground window is null";
			return s_text;
		}

		if (! AttachThreadInput (GetCurrentThreadId(), GetWindowThreadProcessId(hwndForeground, NULL ), true))
		{
			s_text = "Failed to attach thread input";
			return s_text;
		}

		HWND hWndEdit = GetFocus ();
		// tidy up
		AttachThreadInput (GetCurrentThreadId(), GetWindowThreadProcessId(hwndForeground, NULL ), false);
	
		if (hWndEdit != NULL)
		{
			TCHAR * text = new TCHAR[100];
			text[0] = 0;
			SendMessage(hWndEdit, WM_GETTEXT, 99, (LPARAM)text);
			std::wstring w = text;
			s_string = std::string(w.begin(), w.end());
			return s_string.c_str();
		}
		//intptr_t h = (intptr_t)hWndEdit;
		//IntPtr h = (IntPtr)hWndEdit;
		std::ostringstream oss;
		oss << hWndEdit;
		s_string = oss.str();
		return s_string.c_str();
		//String^ test = gcnew String(oss.str().c_str());
		//MessageBox::Show(h.ToString());
	}
}