#include "library.h"

#include <iostream>

void stderr_print() {
    std::wcerr << L"displaying some test in error output";
    std::wcout << L"displaying some test in std output";
}
