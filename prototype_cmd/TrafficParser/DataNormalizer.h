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
// Klasa DataNormalizer nie ma ¿adnych pól (zmiennych), które musia³aby zapamiêtywaæ.
// To oznacza, ¿e nie ma "stanu". Jest tylko zbiorem narzêdzi (funkcji), 
// które przyjmuj¹ dane, coœ licz¹ i zwracaj¹ wynik == STATIC
class DataNormalizer {

public:
    // Ta funkcja zarz¹dza ruchem!
    static float NormalizeValue(const std::string& rawValue, const ColumnDefinition& colDef, bool last_col);

private:
    static float ScaleMinMax(float val, float min, float max);

    static float EncodeEnum(const std::string& val, const ColumnDefinition& colDef, bool last_col);
};