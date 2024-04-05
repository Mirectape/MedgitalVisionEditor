using System;
using System.Collections;
using System.Collections.Generic;
using itk.simple;
using UnityEngine;

public class SimpleITKTestElastix : MonoBehaviour
{
    void Start()
    {
        string movingImagePath = @"C:\Users\Admin\Desktop\ElastixTest\MRIFixed";
        string fixedImagePath = @"C:\Users\Admin\Desktop\ElastixTest\CTMoved";
        //movingImagePath
        try
        {
            ImageSeriesReader seriesReader = new ImageSeriesReader();

            VectorString fixedImageSeriesFilenames = ImageSeriesReader.GetGDCMSeriesFileNames(fixedImagePath);
            VectorString movingImageSeriesFilenames = ImageSeriesReader.GetGDCMSeriesFileNames(movingImagePath);

            seriesReader.SetFileNames(fixedImageSeriesFilenames);
            Image fixedImage = seriesReader.Execute();
            seriesReader.SetFileNames(movingImageSeriesFilenames);
            Image movingImage = seriesReader.Execute();

            ElastixImageFilter elastix = new ElastixImageFilter();

            elastix.SetFixedImage(fixedImage);
            elastix.SetMovingImage(movingImage);

            // Используем стандартный набор параметров для жесткой регистрации
            // Изменяем стандартный набор параметров для жесткой регистрации
            ParameterMap defaultParams = elastix.GetDefaultParameterMap("rigid");
            defaultParams["NumberOfResolutions"] = new VectorString { "3" }; // Использование многоразрешенной стратегии
            defaultParams["Metric"] = new VectorString { "AdvancedMattesMutualInformation" }; // Метрика для мульти-модальной регистрации
            defaultParams["MaximumNumberOfIterations"] = new VectorString { "1024" }; // Увеличение количества итераций
            defaultParams["NumberOfSpatialSamples"] = new VectorString { "2048" }; // Увеличение количества пространственных образцов для метрики
            defaultParams["Optimizer"] = new VectorString { "AdaptiveStochasticGradientDescent" }; // Оптимизатор

            elastix.SetParameterMap(defaultParams);
            elastix.LogToConsoleOn(); // Включаем логирование в консоль

            // Выполняем регистрацию
            elastix.Execute();

            // Получаем результат регистрации
            Image resultImage = elastix.GetResultImage();

            // Получаем итоговые параметры трансформации
            VectorOfParameterMap transformParameterMap = elastix.GetTransformParameterMap();
            PrintParameterMapToConsole(transformParameterMap);

            Debug.Log($"Результат имеет размер: {resultImage.GetWidth()}x{resultImage.GetHeight()}x{resultImage.GetDepth()}");
        }
        catch (Exception e)
        {
            Debug.LogError("Произошла ошибка: " + e.Message);
        }
    }

    // Функция для вывода параметров трансформации в консоль
    void PrintParameterMapToConsole(VectorOfParameterMap parameterMaps)
    {
        for (int i = 0; i < parameterMaps.Count; i++)
        {
            ParameterMap map = parameterMaps[i];
            foreach (KeyValuePair<string, VectorString> entry in map)
            {
                string key = entry.Key;
                VectorString values = entry.Value;
                string valueString = string.Join(", ", values.ToArray());
                Debug.Log($"{key}: {valueString}");
            }
        }
    }
}
