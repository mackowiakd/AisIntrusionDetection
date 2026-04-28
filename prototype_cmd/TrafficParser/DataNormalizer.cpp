/*
* Klasa/funkcje do zamiany np. tekstu "TCP" na 1.0 i skalowania wartoœci (¿eby wszystko by³o miêdzy 0 a 1).
*/

#include "ParserApi.h"
#include "DataNormalizer.h"


// Zauwa¿ brak s³owa 'static' tutaj!
float DataNormalizer::NormalizeValue(const std::string& rawValue, const ColumnDefinition& colDef, bool last_col) {
    if (colDef.type == ColumnType::REAL) {
        float val = std::log(1.0f + std::stof(rawValue));
        return ScaleMinMax(val, colDef.minVal, colDef.maxVal);
    }
    else if (colDef.type == ColumnType::ENUM || colDef.type == ColumnType::LABEL) {
        return EncodeEnum(rawValue, colDef, last_col);
    }
    return 0.0f;
}
float DataNormalizer:: ScaleMinMax(float val, float min, float max) {
    if (max == min) return 0.0f; // Zabezpieczenie przed dzieleniem przez zero
    return (val - min) / (max - min);
}
 float DataNormalizer::  EncodeEnum(const std::string& val, const ColumnDefinition& colDef, bool last_col) {
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