#pragma once

#include <string>
#include <vector>
#include <cmath>

enum class ColumnType { REAL, ENUM, LABEL };

struct ColumnDefinition {
    std::string name;
    ColumnType type;

    // Dla typu REAL (do szukania Min/Max)
    float minVal = 3.402823466e+38F; // Max float (nieskoñczonoœæ)
    float maxVal = -3.402823466e+38F; // Min float

    // Dla typu ENUM (przechowuje opcje np. "tcp", "udp", "icmp")
    std::vector<std::string> enumLabels;
};


// Klasa pomocnicza do "czyszczenia" i skalowania danych
class DataNormalizer {

public:
    // Ta funkcja zarz¹dza ruchem!
    static float NormalizeValue(const std::string& rawValue, const ColumnDefinition& colDef, bool last_col) {
        if (colDef.type == ColumnType::REAL) {
            // ZMIANA TUTAJ: Logarytm t³umi¹cy gigantyczne wartoœci odstaj¹ce!
           
            float val = std::log(1.0f + std::stof(rawValue));
            return ScaleMinMax(val, colDef.minVal, colDef.maxVal);
        }
        else if (colDef.type == ColumnType::ENUM || colDef.type == ColumnType::LABEL ) {
            return EncodeEnum(rawValue, colDef, last_col);
        }
        return 0.0f;
    }

private:
    static float ScaleMinMax(float val, float min, float max) {
        if (max == min) return 0.0f; // Zabezpieczenie przed dzieleniem przez zero
        return (val - min) / (max - min);
    }

    static float EncodeEnum(const std::string& val, const ColumnDefinition& colDef, bool last_col) {
        // TODO: Szukasz 'val' w wektorze 'enumValues' i zwracasz: indeks / (rozmiar - 1)
        // Pamiêtaj zabezpieczyæ sytuacjê, gdy wektor ma tylko 1 element (¿eby nie dzieliæ przez zero)
		if (colDef.enumLabels.size() == 1) return 0.0f; // Jeœli jest tylko jedna opcja, zawsze zwracamy 0.0f
        // Szukamy elementu
        if (last_col) {
            if (val == "normal") return 0.0f;
			else return 1.0f;
        }
        auto it = std::find(colDef.enumLabels.begin(), colDef.enumLabels.end(), val);

        // Zabezpieczenie: co jeœli w pliku z danymi trafi siê wartoœæ, której nie ma w definicji?
        if (it == colDef.enumLabels.end()) return 0.0f;

        // Liczymy który to indeks (0, 1, 2...)
        int index = std::distance(colDef.enumLabels.begin(), it);

        return static_cast<float>(index) / (colDef.enumLabels.size() - 1);
    }
};