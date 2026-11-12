cmake_minimum_required(VERSION 3.28)
project(ecat_shim C)

add_subdirectory(../SOEM soem EXCLUDE_FROM_ALL)
add_library(ecat_shim SHARED ecat_shim.c)
target_link_libraries(ecat_shim PRIVATE soem)