cmake_minimum_required(VERSION 3.23)
project(Ascertain.Backbone)

set(CMAKE_CXX_STANDARD 23)

add_library(Ascertain.Backbone SHARED library.cpp)

# TODO : Implement static linking instead of dynamic to simplify deployment