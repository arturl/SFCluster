#pragma once

namespace ImageRecognizerLib
{
    class CNTKImageRecognizerState;

    class CNTKImageRecognizer
    {
        CNTKImageRecognizerState* state;
        std::vector<std::wstring> classNames;
        std::wstring classifyImage(const uint8_t* image_data, size_t image_data_len);

    public:
        CNTKImageRecognizer(const std::wstring & modelFile, const std::wstring & classesFile);
        ~CNTKImageRecognizer();
        std::wstring RecognizeObject(const uint8_t* bytes, size_t size);
        uint32_t GetRequiredWidth();
        uint32_t GetRequiredHeight();
        uint32_t GetRequiredChannels();
    };
}
