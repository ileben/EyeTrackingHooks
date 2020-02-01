#include "stdafx.h"

#include "EyeTrackingCLR.h"

using namespace System; // Object
using namespace System::IO; // Path
using namespace System::Reflection; // Assembly
using namespace System::Windows::Forms;

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

extern "C"
{
	bool isInitialized = false;
	bool followGaze = false;
     
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
}