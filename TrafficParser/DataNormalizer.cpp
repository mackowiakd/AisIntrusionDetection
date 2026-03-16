/*
* Klasa/funkcje do zamiany np. tekstu "TCP" na 1.0 i skalowania wartoúci (øeby wszystko by≥o miÍdzy 0 a 1).
*/

#include "ParserApi.h"
#include "DataNormalizer.h"


// Funkcja pomocnicza do ciÍcia stringa po przecinku(warto wrzuciÊ na gÛrÍ pliku lub do utilsÛw)
std::vector<std::string> SplitByComma(const std::string & line) {
    std::vector<std::string> result;
    std::stringstream ss(line);
    std::string item;
    while (std::getline(ss, item, ',')) {
        // Tu moøna ew. usunπÊ bia≥e znaki/apostrofy z 'item'
        result.push_back(item);
    }
    return result;
}

extern "C" __declspec(dllexport) int LoadAndParseDataset(const char* filePath, float* outputArray, int maxRows, int featuresCount)
{
    std::ifstream file(filePath);
    if (!file.is_open()) return -1; // B≥πd otwarcia pliku

    std::string line;
    std::vector<ColumnDefinition> schema;
    std::vector<std::vector<std::string>> rawData;

    // ==========================================
    // ETAP 1: PARSOWANIE SCHEMATU (NAG£”WKA)
    // ==========================================
    while (std::getline(file, line)) {
        if (line.empty()) continue; // pomijamy puste linie

        if (line.find("@attribute") == 0) {
            // TODO: Zaimplementuj wyciπganie typu kolumny.
            // Przyk≥ad: jeúli linia zawiera "real", dodajesz do 'schema' obiekt typu REAL.
            // Jeúli linia zawiera '{', wycinasz wartoúci po przecinku do wektora enumValues.

            ColumnDefinition colDef;
            // colDef.type = ... 
            schema.push_back(colDef);
        }
        else if (line.find("@data") == 0) {
            // ZNALEèLIåMY @data! To koniec nag≥Ûwka, przerywamy pÍtlÍ i idziemy do Etapu 2.
            break;
        }
    }

    // ==========================================
    // ETAP 2: CZYTANIE DANYCH DO RAM + SZUKANIE MIN/MAX
    // ==========================================
    int currentRow = 0;
    while (std::getline(file, line) && currentRow < maxRows) {
        if (line.empty() || line[0] == '%') continue; // pomijamy puste i komentarze

        // Tniemy liniÍ ("0,tcp,http,SF...") na pojedyncze s≥owa
        std::vector<std::string> rowValues = SplitByComma(line);

        // SZUKANIE MIN/MAX W LOCIE:
        // Przechodzimy przez wyciÍte s≥owa i aktualizujemy nasz 'schema'
        for (int i = 0; i < rowValues.size(); i++) {
            if (schema[i].type == ColumnType::REAL) {
                float val = std::stof(rowValues[i]); // String to Float
                if (val < schema[i].minVal) schema[i].minVal = val;
                if (val > schema[i].maxVal) schema[i].maxVal = val;
            }
        }

        // Zapisujemy wiersz w pamiÍci RAM
        rawData.push_back(rowValues);
        currentRow++;
    }

    // ==========================================
    // ETAP 3: NORMALIZACJA I EKSPORT DO C#
    // ==========================================
    // Teraz znamy juø wszystkie ekstrema (Min i Max)! Moøemy bezpiecznie skalowaÊ.

    for (int r = 0; r < rawData.size(); r++) {
        for (int c = 0; c < rawData[r].size(); c++) {

            // Podajemy surowy string oraz zaktualizowanπ definicjÍ kolumny (z gotowym Min/Max) do naszego "kalkulatora"
            float normalizedValue = DataNormalizer::NormalizeValue(rawData[r][c], schema[c]);

            // Wpisujemy wynik do p≥askiej tablicy C#
            // WzÛr r * featuresCount + c mapuje wiersze i kolumny na 1-wymiarowπ tablicÍ
            outputArray[r * featuresCount + c] = normalizedValue;
        }
    }

    file.close();
    return currentRow; // Zwracamy C# informacjÍ ile rzÍdÛw uda≥o siÍ przerobiÊ
}