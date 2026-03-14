//funkcje na zewn¹trz (dla C#)

#include "ParserApi.h"

// Funkcja eksportowana dla C#
extern "C" __declspec(dllexport) void ProcessNetworkRecord(const char* rawRecord, float* outputVector, int vectorSize)
{
    // Tutaj w przysz³oœci bêdzie logika ciêcia stringa z CSV
    // Na razie wrzuæmy przyk³adowe dane do tablicy (udajemy, ¿e znormalizowaliœmy pakiet)

    if (vectorSize >= 3) {
        outputVector[0] = 0.5f; // Udajemy znormalizowany czas trwania
        outputVector[1] = 1.0f; // Udajemy TCP
        outputVector[2] = 0.0f; // Udajemy brak flag b³êdu
    }
}