#include "stdafx.h"
#include "CNTKImageRecognizer.h"

#include "CNTKLibrary.h"

namespace ImageRecognizerLib
{
    class CNTKImageRecognizerState
    {
    public:
        CNTK::DeviceDescriptor evalDevice = CNTK::DeviceDescriptor::CPUDevice();
        CNTK::FunctionPtr model;
        CNTK::Variable inputVar;
        CNTK::NDShape inputShape;
    };
}

using namespace ImageRecognizerLib;
using namespace Microsoft::MSR::CNTK;

#include "utils.inl"

std::wstring CNTKImageRecognizer::classifyImage(const uint8_t* image_data, size_t image_data_len, int loggingLevel)
{
    std::wstring log = L"";
    try
    {
        if(loggingLevel==2) log += L"1 ";

        // Prepare the input vector and convert it to the correct color scheme (BBB ... GGG ... RRR)
        size_t resized_image_data_len = GetRequiredWidth() * GetRequiredHeight() * GetRequiredChannels();
        std::vector<uint8_t> image_data_array(resized_image_data_len);
        memcpy_s(image_data_array.data(), image_data_len, image_data, resized_image_data_len);
        std::vector<float> rawFeatures = get_features(image_data_array.data(), GetRequiredWidth(), GetRequiredHeight());

        if (loggingLevel == 2) log += L"2 ";

        // Prepare the input layer of the computation graph
        // Most of the work is putting rawFeatures into CNTK's data representation format
        std::unordered_map<CNTK::Variable, CNTK::ValuePtr> inputLayer = {};

        auto features = CNTK::Value::CreateBatch<float>(this->state->inputShape, rawFeatures, this->state->evalDevice, false);
        this->state->inputVar = this->state->model->Arguments()[0];
        inputLayer.insert({ this->state->inputVar, features });

        if (loggingLevel == 2) log += L"3 ";

        // Prepare the output layer of the computation graph
        // For this a NULL blob will be placed into the output layer
        // so that CNTK can place its own datastructure there
        std::unordered_map<CNTK::Variable, CNTK::ValuePtr> outputLayer = {};
        CNTK::Variable outputVar = this->state->model->Output();
        CNTK::NDShape outputShape = outputVar.Shape();
        size_t possibleClasses = outputShape.Dimensions()[0];

        if (loggingLevel == 2) log += L"4 ";

        std::vector<float> rawOutputs(possibleClasses);
        auto outputs = CNTK::Value::CreateBatch<float>(outputShape, rawOutputs, this->state->evalDevice, false);
        outputLayer.insert({ outputVar, NULL });

        if (loggingLevel == 2) log += L"5 ";

        // Evaluate the image and extract the results (which will be a [ #classes x 1 x 1 ] tensor)
        this->state->model->Evaluate(inputLayer, outputLayer, this->state->evalDevice);

        if (loggingLevel == 2) log += L"6 ";

        CNTK::ValuePtr outputVal = outputLayer[outputVar];
        std::vector<std::vector<float>> resultsWrapper;
        std::vector<float> results;

        outputVal.get()->CopyVariableValueTo(outputVar, resultsWrapper);
        results = resultsWrapper[0];

        if (loggingLevel == 2) log += L"7 ";

        // Map the results to the string representation of the class
        int64_t image_class = find_class(results);

        if (loggingLevel == 2) log += L"8 ";

        return classNames.at(image_class);
    }
    catch (const std::exception& ex)
    {
        return L"exception in classifyImage. Log:" + log + strtowstr(ex.what());
    }
}

uint32_t CNTKImageRecognizer::GetRequiredWidth()
{
    return (uint32_t)this->state->inputShape[0];
}

uint32_t CNTKImageRecognizer::GetRequiredHeight()
{
    return (uint32_t)this->state->inputShape[1];
}

uint32_t CNTKImageRecognizer::GetRequiredChannels()
{
    return (uint32_t)this->state->inputShape[2];
}

CNTKImageRecognizer::CNTKImageRecognizer(const std::wstring & modelFile, const std::wstring & classesFile)
{
    this->state = new CNTKImageRecognizerState();

    this->state->model = CNTK::Function::Load(modelFile, this->state->evalDevice);

    // List out all the outputs and their indexes
    // The probability output is usually listed as 'z' and is 
    // usually the last layer
    size_t z_index = this->state->model->Outputs().size() - 1;

    // Modify the in-memory model to use the z layer as the actual output
    auto z_layer = this->state->model->Outputs()[z_index];
    this->state->model = CNTK::Combine({ z_layer.Owner() });

    // Extract information about what the model accepts as input
    this->state->inputVar = this->state->model->Arguments()[0];
    // Shape contains image [width, height, depth] respectively
    this->state->inputShape = this->state->inputVar.Shape();

    // Load the class names
    classNames = read_class_names(classesFile);
}

CNTKImageRecognizer::~CNTKImageRecognizer()
{
    delete state;
}

std::wstring CNTKImageRecognizer::RecognizeObject(const uint8_t* bytes, size_t size, int loggingLevel)
{
#if 0
    // The data we've got is in RGBA format. We should convert it to BGR
    std::vector<uint8_t> rgb((size / 4) * 3);
    const uint8_t* rgba = bytes;

    uint32_t i = 0;
    for (uint32_t j = 0; j < size;)
    {
        uint32_t r = j++;  // R
        uint32_t g = j++;  // G
        uint32_t b = j++;  // B
        uint32_t a = j++;  // A (skipped)

        rgb[i++] = rgba[r];
        rgb[i++] = rgba[g];
        rgb[i++] = rgba[b];
    }

    auto image_class = classifyImage(rgb.data(), rgb.size(), loggingLevel);
#else
    auto image_class = classifyImage(bytes, size, loggingLevel);
#endif
    return image_class;
}
