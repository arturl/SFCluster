//#include "stdafx.h"
#include <msclr/marshal_cppstd.h>
#include "CNTKImageRecognizer.h"

using namespace msclr::interop;

namespace ImageRecognizerLib
{
    public ref class CNTKImageRecognizerWrappper
    {
        CNTKImageRecognizer* recognizer;

    public:
        CNTKImageRecognizerWrappper(System::String^ modelFile, System::String^  classesFile)
        {
            recognizer = new CNTKImageRecognizer(marshal_as<std::wstring>(modelFile), marshal_as<std::wstring>(classesFile));
        }

        ~CNTKImageRecognizerWrappper()
        {
            delete recognizer;
        }

        System::String^ RecognizeObject(cli::array<uint8_t>^ data)
        {
            pin_ptr<uint8_t> pptr = &data[0];
            uint8_t* p = pptr;
            auto objName = recognizer->RecognizeObject(p, data->Length);

            return gcnew System::String(objName.data());
        }

        uint32_t GetRequiredWidth()     { return recognizer->GetRequiredWidth(); }
        uint32_t GetRequiredHeight()    { return recognizer->GetRequiredHeight(); }
        uint32_t GetRequiredChannels()  { return recognizer->GetRequiredChannels(); }
    };
}
