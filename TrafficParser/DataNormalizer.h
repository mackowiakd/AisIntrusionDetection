#pragma once

#include <string>
#include <vector>

enum class ColumnType { REAL, ENUM, LABEL };

struct ColumnDefinition {
    std::string name;
    ColumnType type;

    // Dla typu REAL (do szukania Min/Max)
    float minVal = 3.402823466e+38F; // Max float (nieskoñczonoœæ)
    float maxVal = -3.402823466e+38F; // Min float

    // Dla typu ENUM (przechowuje opcje np. "tcp", "udp", "icmp")
    std::vector<std::string> enumValues;
};

// Klasa pomocnicza do "czyszczenia" i skalowania danych
class DataNormalizer {

public:
    // Ta funkcja zarz¹dza ruchem!
    static float NormalizeValue(const std::string& rawValue, const ColumnDefinition& colDef) {
        if (colDef.type == ColumnType::REAL) {
            float val = std::stof(rawValue); // konwersja string -> float
            return ScaleMinMax(val, colDef.minVal, colDef.maxVal);
        }
        else if (colDef.type == ColumnType::ENUM || colDef.type == ColumnType::LABEL) {
            return EncodeEnum(rawValue, colDef.enumValues);
        }
        return 0.0f;
    }

private:
    static float ScaleMinMax(float val, float min, float max) {
        if (max == min) return 0.0f; // Zabezpieczenie przed dzieleniem przez zero
        return (val - min) / (max - min);
    }

    static float EncodeEnum(const std::string& val, const std::vector<std::string>& enumValues) {
        // TODO: Szukasz 'val' w wektorze 'enumValues' i zwracasz: indeks / (rozmiar - 1)
        // Pamiêtaj zabezpieczyæ sytuacjê, gdy wektor ma tylko 1 element (¿eby nie dzieliæ przez zero)
    }
};