cmake_minimum_required (VERSION 2.6)

project (AmppControlSample)

add_executable(AmppControlSample AmppControlSample.cpp BearerToken.cpp PushNotificationServer.cpp Util.cpp AmppControlUtil.cpp)
TARGET_LINK_LIBRARIES(AmppControlSample pthread crypto ssl curl)

