#pragma once
#include <fstream>

#include <sstream>



// U¿ywamy extern "C", ¿eby unikn¹æ "manglingu" nazw funkcji w C++ i u³atwiæ DllImport w C#
extern "C" {
    // Funkcja ³aduj¹ca ca³y plik CSV i wpisuj¹ca znormalizowane dane do p³askiej tablicy outputArray
    __declspec(dllexport) int LoadAndParseDataset(
        const char* filePath,   // Œcie¿ka do pliku od C#
        float* outputArray,     // Pusta tablica przygotowana przez C# (wskaŸnik na jej pocz¹tek)
        int maxRows,            // Ile maksymalnie wierszy C# chce wczytaæ
        int featuresCount       // Z ilu kolumn sk³ada siê jeden "Antygen"
    );
}