//funkcje na zewn¹trz (dla C#)



#include "ParserApi.h"
#include "DataNormalizer.h"
#include <fstream>
#include <string>
#include <sstream>
#include <vector>
#include <cmath>


// Funkcja pomocnicza do ciêcia stringa po przecinku(warto wrzuciæ na górê pliku lub do utilsów)
std::vector<std::string> SplitByComma(const std::string& line, char separator) {
    std::vector<std::string> result;
    std::stringstream ss(line);
    std::string item;
    while (std::getline(ss, item, separator)) {
        // Tu mo¿na ew. usun¹æ bia³e znaki/apostrofy z 'item'
        result.push_back(item);
    }
    return result;
}

extern "C" __declspec(dllexport) int LoadAndParseDataset(const char* filePath, float* outputArray, int maxRows, int featuresCount)
{
    std::ifstream file(filePath);
   
    
    if (!file.is_open()) return -1 ; // B³¹d otwarcia pliku

    std::string line;
    std::vector<ColumnDefinition> schema;
    std::vector<std::string> labels;
    std::vector<std::vector<std::string>> rawData;

    ColumnDefinition colDef;
    // ==========================================
    // ETAP 1: PARSOWANIE SCHEMATU (NAG£ÓWKA)
    // ==========================================
    while (std::getline(file, line)) {
        if (line.empty()) continue; // pomijamy puste linie

        if (line.find("@attribute") == 0) {
            // 1. Twój szybki strza³ po nazwê kolumny
            labels = SplitByComma(line, ' ');
            colDef.name = labels[1];

            // 2. Czy to ENUM? (Szukamy klamerek zamiast liczyæ przecinki w ca³ej linii)
            size_t start = line.find('{');
            size_t end = line.find('}');

            if (start != std::string::npos && end != std::string::npos) {
                colDef.type = ColumnType::ENUM;

                // Wycinamy TYLKO to, co w klamerkach: np. "'tcp','udp','icmp'"
                std::string enumContent = line.substr(start + 1, end - start - 1);

                // 3. TERAZ tniemy po przecinku, ale tylko tê ma³¹ wyciêt¹ czêœæ!
                labels = SplitByComma(enumContent, ',');

                // Zaczynamy od i = 0, bo tu s¹ ju¿ same czyste enumy
                for (int i = 0; i < labels.size(); i++) {
                    std::string cleanOpt = labels[i];
                    // Usuwamy apostrofy
                    cleanOpt.erase(std::remove(cleanOpt.begin(), cleanOpt.end(), '\''), cleanOpt.end());
                    // Usuwamy spacje (niektóre opcje w ARFF maj¹ spacjê po przecinku)
                    cleanOpt.erase(std::remove(cleanOpt.begin(), cleanOpt.end(), ' '), cleanOpt.end());
                    colDef.enumLabels.push_back(labels[i]);
                }
            }
            else {
                // Jeœli nie ma klamerek, to musi byæ REAL
                colDef.type = ColumnType::REAL;
            }

            schema.push_back(colDef);
            colDef = ColumnDefinition(); // reset
        }
        else if (line.find("@data") == 0) {
            // ZNALELIŒMY @data! To koniec nag³ówka, przerywamy pêtlê i idziemy do Etapu 2.
            break;
        }
    }

    // ==========================================
    // ETAP 2: CZYTANIE DANYCH DO RAM + SZUKANIE MIN/MAX
    // ==========================================
    int currentRow = 0;
    while (std::getline(file, line) && currentRow < maxRows) {
        if (line.empty() || line[0] == '%') continue; // pomijamy puste i komentarze

        // Tniemy liniê ("0,tcp,http,SF...") na pojedyncze s³owa
        std::vector<std::string> rowValues = SplitByComma(line, ',');

        // SZUKANIE MIN/MAX W LOCIE:
        // Przechodzimy przez wyciête s³owa i aktualizujemy nasz 'schema'
        for (int i = 0; i < rowValues.size(); i++) {
            if (schema[i].type == ColumnType::REAL) {

                // ZMIANA TUTAJ: Logarytm t³umi¹cy gigantyczne wartoœci odstaj¹ce!
                float val = std::log(1.0f + std::stof(rowValues[i]));
                if (val < schema[i].minVal) schema[i].minVal = val;
                if (val > schema[i].maxVal) schema[i].maxVal = val;
            }
        }

        // Zapisujemy wiersz w pamiêci RAM
        rawData.push_back(rowValues);
        currentRow++;
    }

    // ==========================================
    // ETAP 3: NORMALIZACJA I EKSPORT DO C#
    // ==========================================
    // Teraz znamy ju¿ wszystkie ekstrema (Min i Max)! Mo¿emy bezpiecznie skalowaæ.

    for (int r = 0; r < rawData.size(); r++) {
        // Musimy uwa¿aæ, czy iloœæ kolumn w wierszu nie przekracza naszej zadeklarowanej featuresCount
        int colsToProcess = std::min((int)rawData[r].size(), featuresCount);

        for (int c = 0; c < colsToProcess; c++) {
            // Zabezpieczenie - usuwamy apostrofy ' z wartoœci (np. z 'normal')
            std::string rawVal = rawData[r][c];
            rawVal.erase(std::remove(rawVal.begin(), rawVal.end(), '\''), rawVal.end());

            float normalizedValue = DataNormalizer::NormalizeValue(rawVal, schema[c], bool(c==colsToProcess-1));

            // Wpisujemy wynik do p³askiej tablicy C#
            outputArray[r * featuresCount + c] = normalizedValue;
        }
    }

    file.close();
    return currentRow;
}