#pragma once

extern "C" {
    __declspec(dllexport)
    void stderr_print(const wchar_t* content);
}
