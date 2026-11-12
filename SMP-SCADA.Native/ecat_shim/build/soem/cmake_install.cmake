# Install script for directory: C:/Users/w.burke/source/repos/SMP-SCADA.Native/SOEM

# Set the install prefix
if(NOT DEFINED CMAKE_INSTALL_PREFIX)
  set(CMAKE_INSTALL_PREFIX "C:/Program Files (x86)/ecat_shim")
endif()
string(REGEX REPLACE "/$" "" CMAKE_INSTALL_PREFIX "${CMAKE_INSTALL_PREFIX}")

# Set the install configuration name.
if(NOT DEFINED CMAKE_INSTALL_CONFIG_NAME)
  if(BUILD_TYPE)
    string(REGEX REPLACE "^[^A-Za-z0-9_]+" ""
           CMAKE_INSTALL_CONFIG_NAME "${BUILD_TYPE}")
  else()
    set(CMAKE_INSTALL_CONFIG_NAME "Release")
  endif()
  message(STATUS "Install configuration: \"${CMAKE_INSTALL_CONFIG_NAME}\"")
endif()

# Set the component getting installed.
if(NOT CMAKE_INSTALL_COMPONENT)
  if(COMPONENT)
    message(STATUS "Install component: \"${COMPONENT}\"")
    set(CMAKE_INSTALL_COMPONENT "${COMPONENT}")
  else()
    set(CMAKE_INSTALL_COMPONENT)
  endif()
endif()

# Is this installation the result of a crosscompile?
if(NOT DEFINED CMAKE_CROSSCOMPILING)
  set(CMAKE_CROSSCOMPILING "FALSE")
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/w.burke/source/repos/SMP-SCADA.Native/ecat_shim/build/soem/Debug/soem.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/w.burke/source/repos/SMP-SCADA.Native/ecat_shim/build/soem/Release/soem.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Mm][Ii][Nn][Ss][Ii][Zz][Ee][Rr][Ee][Ll])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/w.burke/source/repos/SMP-SCADA.Native/ecat_shim/build/soem/MinSizeRel/soem.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ww][Ii][Tt][Hh][Dd][Ee][Bb][Ii][Nn][Ff][Oo])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/w.burke/source/repos/SMP-SCADA.Native/ecat_shim/build/soem/RelWithDebInfo/soem.lib")
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    include("C:/Users/w.burke/source/repos/SMP-SCADA.Native/ecat_shim/build/soem/CMakeFiles/soem.dir/install-cxx-module-bmi-Debug.cmake" OPTIONAL)
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
    include("C:/Users/w.burke/source/repos/SMP-SCADA.Native/ecat_shim/build/soem/CMakeFiles/soem.dir/install-cxx-module-bmi-Release.cmake" OPTIONAL)
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Mm][Ii][Nn][Ss][Ii][Zz][Ee][Rr][Ee][Ll])$")
    include("C:/Users/w.burke/source/repos/SMP-SCADA.Native/ecat_shim/build/soem/CMakeFiles/soem.dir/install-cxx-module-bmi-MinSizeRel.cmake" OPTIONAL)
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ww][Ii][Tt][Hh][Dd][Ee][Bb][Ii][Nn][Ff][Oo])$")
    include("C:/Users/w.burke/source/repos/SMP-SCADA.Native/ecat_shim/build/soem/CMakeFiles/soem.dir/install-cxx-module-bmi-RelWithDebInfo.cmake" OPTIONAL)
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(EXISTS "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/soemConfig.cmake")
    file(DIFFERENT _cmake_export_file_changed FILES
         "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/soemConfig.cmake"
         "C:/Users/w.burke/source/repos/SMP-SCADA.Native/ecat_shim/build/soem/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/soemConfig.cmake")
    if(_cmake_export_file_changed)
      file(GLOB _cmake_old_config_files "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/soemConfig-*.cmake")
      if(_cmake_old_config_files)
        string(REPLACE ";" ", " _cmake_old_config_files_text "${_cmake_old_config_files}")
        message(STATUS "Old export file \"$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/soemConfig.cmake\" will be replaced.  Removing files [${_cmake_old_config_files_text}].")
        unset(_cmake_old_config_files_text)
        file(REMOVE ${_cmake_old_config_files})
      endif()
      unset(_cmake_old_config_files)
    endif()
    unset(_cmake_export_file_changed)
  endif()
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/w.burke/source/repos/SMP-SCADA.Native/ecat_shim/build/soem/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/soemConfig.cmake")
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/w.burke/source/repos/SMP-SCADA.Native/ecat_shim/build/soem/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/soemConfig-debug.cmake")
  endif()
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Mm][Ii][Nn][Ss][Ii][Zz][Ee][Rr][Ee][Ll])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/w.burke/source/repos/SMP-SCADA.Native/ecat_shim/build/soem/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/soemConfig-minsizerel.cmake")
  endif()
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ww][Ii][Tt][Hh][Dd][Ee][Bb][Ii][Nn][Ff][Oo])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/w.burke/source/repos/SMP-SCADA.Native/ecat_shim/build/soem/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/soemConfig-relwithdebinfo.cmake")
  endif()
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/w.burke/source/repos/SMP-SCADA.Native/ecat_shim/build/soem/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/soemConfig-release.cmake")
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/include/soem" TYPE FILE FILES
    "C:/Users/w.burke/source/repos/SMP-SCADA.Native/SOEM/include/soem/ec_base.h"
    "C:/Users/w.burke/source/repos/SMP-SCADA.Native/SOEM/include/soem/ec_coe.h"
    "C:/Users/w.burke/source/repos/SMP-SCADA.Native/SOEM/include/soem/ec_config.h"
    "C:/Users/w.burke/source/repos/SMP-SCADA.Native/SOEM/include/soem/ec_dc.h"
    "C:/Users/w.burke/source/repos/SMP-SCADA.Native/SOEM/include/soem/ec_eoe.h"
    "C:/Users/w.burke/source/repos/SMP-SCADA.Native/SOEM/include/soem/ec_foe.h"
    "C:/Users/w.burke/source/repos/SMP-SCADA.Native/SOEM/include/soem/ec_main.h"
    "C:/Users/w.burke/source/repos/SMP-SCADA.Native/SOEM/include/soem/ec_print.h"
    "C:/Users/w.burke/source/repos/SMP-SCADA.Native/SOEM/include/soem/ec_soe.h"
    "C:/Users/w.burke/source/repos/SMP-SCADA.Native/SOEM/include/soem/ec_type.h"
    "C:/Users/w.burke/source/repos/SMP-SCADA.Native/SOEM/include/soem/soem.h"
    "C:/Users/w.burke/source/repos/SMP-SCADA.Native/SOEM/osal/osal.h"
    "C:/Users/w.burke/source/repos/SMP-SCADA.Native/ecat_shim/build/soem/include/soem/ec_options.h"
    )
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/scripts" TYPE FILE FILES "C:/Users/w.burke/source/repos/SMP-SCADA.Native/SOEM/scripts/eniconv.py")
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/w.burke/source/repos/SMP-SCADA.Native/SOEM/cmake/AddENI.cmake")
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/." TYPE FILE FILES
    "C:/Users/w.burke/source/repos/SMP-SCADA.Native/SOEM/README.md"
    "C:/Users/w.burke/source/repos/SMP-SCADA.Native/SOEM/LICENSE.md"
    )
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/include/soem" TYPE FILE FILES
    "C:/Users/w.burke/source/repos/SMP-SCADA.Native/SOEM/osal/win32/osal_defs.h"
    "C:/Users/w.burke/source/repos/SMP-SCADA.Native/SOEM/oshw/win32/nicdrv.h"
    )
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/bin" TYPE PROGRAM FILES
    "D:/Visual Studio/VC/Redist/MSVC/14.52.37025/x64/Microsoft.VC145.CRT/msvcp140.dll"
    "D:/Visual Studio/VC/Redist/MSVC/14.52.37025/x64/Microsoft.VC145.CRT/msvcp140_1.dll"
    "D:/Visual Studio/VC/Redist/MSVC/14.52.37025/x64/Microsoft.VC145.CRT/msvcp140_2.dll"
    "D:/Visual Studio/VC/Redist/MSVC/14.52.37025/x64/Microsoft.VC145.CRT/msvcp140_atomic_wait.dll"
    "D:/Visual Studio/VC/Redist/MSVC/14.52.37025/x64/Microsoft.VC145.CRT/msvcp140_codecvt_ids.dll"
    "D:/Visual Studio/VC/Redist/MSVC/14.52.37025/x64/Microsoft.VC145.CRT/vcruntime140_1.dll"
    "D:/Visual Studio/VC/Redist/MSVC/14.52.37025/x64/Microsoft.VC145.CRT/vcruntime140.dll"
    "D:/Visual Studio/VC/Redist/MSVC/14.52.37025/x64/Microsoft.VC145.CRT/concrt140.dll"
    )
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/bin" TYPE DIRECTORY FILES "")
endif()

string(REPLACE ";" "\n" CMAKE_INSTALL_MANIFEST_CONTENT
       "${CMAKE_INSTALL_MANIFEST_FILES}")
if(CMAKE_INSTALL_LOCAL_ONLY)
  file(WRITE "C:/Users/w.burke/source/repos/SMP-SCADA.Native/ecat_shim/build/soem/install_local_manifest.txt"
     "${CMAKE_INSTALL_MANIFEST_CONTENT}")
endif()
