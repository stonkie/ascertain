#include "library.h"

#include <iostream>

void stderr_print(const wchar_t* content) {
    std::wstring_view view(content);

    std::wcerr << view;
}
