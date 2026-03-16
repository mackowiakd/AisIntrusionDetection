//funkcje na zewn¹trz (dla C#)



#include "ParserApi.h"
#include "DataNormalizer.h"
#include <fstream>
#include <string>
#include <sstream>
#include <vector>

extern "C" __declspec(dllexport) int LoadAndParseDataset(const char* filePath, float* outputArray, int maxRows, int featuresCount)
{
    std::ifstream file(filePath);
    if (!file.is_open()) {
        return -1; // B³¹d otwarcia pliku
    }

    std::string line;
    int currentRow = 0;

    // TODO: G³ówna pêtla wczytuj¹ca plik
    while (std::getline(file, line) && currentRow < maxRows) {

        // Wskazówka: U¿yj std::stringstream(line) i std::getline z u¿yciem przecinka ',' 
        // jako delimitera, ¿eby poci¹æ liniê na poszczególne kolumny.

        // Przyk³ad zapisu do p³askiej tablicy (któr¹ C# widzi jako wielowymiarow¹):
        // outputArray[currentRow * featuresCount + 0] = DataNormalizer::ScaleMinMax(...);
        // outputArray[currentRow * featuresCount + 1] = DataNormalizer::EncodeProtocol(...);

        currentRow++;
    }

    file.close();

    // Zwracamy C# informacjê, ile faktycznie wierszy (Antygenów) uda³o nam siê wczytaæ
    return currentRow;
}