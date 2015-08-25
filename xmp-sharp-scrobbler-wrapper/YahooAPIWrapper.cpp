#include "Stdafx.h"

#include <msclr\auto_gcroot.h>

#using "xmp-sharp-scrobbler-managed.dll"

using namespace System::Runtime::InteropServices; // Marshal

class YahooAPIWrapperPrivate
{
    public: msclr::auto_gcroot<YahooAPI^> yahooAPI;
};

class __declspec(dllexport) YahooAPIWrapper
{
    private: YahooAPIWrapperPrivate* _private;

    public: YahooAPIWrapper()
    {
        _private = new YahooAPIWrapperPrivate();
        _private->yahooAPI = gcnew YahooAPI();
    }
    
    public: double GetBid(const char* symbol)
    {
        return _private->yahooAPI->GetBid(gcnew System::String(symbol));
    }

    public: double GetAsk(const char* symbol)
    {
        return _private->yahooAPI->GetAsk(gcnew System::String(symbol));
    }
    
    public: const char* GetCapitalization(const char* symbol)
    {
        System::String^ managedCapi = _private->yahooAPI->GetCapitalization(gcnew System::String(symbol));
    
        return (const char*)Marshal::StringToHGlobalAnsi(managedCapi).ToPointer();
    }
    
    public: const char** GetValues(const char* symbol, const char* fields)
    {
        cli::array<System::String^>^ managedValues = _private->yahooAPI->GetValues(gcnew System::String(symbol), gcnew System::String(fields));
        
        const char** unmanagedValues = new const char*[managedValues->Length];
        
        for (int i = 0; i < managedValues->Length; ++i)
        {
            unmanagedValues[i] = (const char*)Marshal::StringToHGlobalAnsi(managedValues[i]).ToPointer();
        }
        
        return unmanagedValues;
    }
    
    public: ~YahooAPIWrapper()
    {
        delete _private;
    }
};