//
// Copyright Grass Valley
//

#include "Util.h"

#include <chrono>
#include <ctime>
#include <iomanip>
#include <iostream>
#include <random>
#include <sstream>

// This may not be the best implementation to guarantee UUID uniqueness.
// Please feel free to use any other librairy of your choice to meet
// this criteria.
std::string getUuid()
{
    static std::random_device              rd;
    static std::mt19937                    gen( rd() );
    static std::uniform_int_distribution<> dis( 0, 15 );
    static std::uniform_int_distribution<> dis2( 8, 11 );

    std::stringstream ss;
    int i;
    ss << std::hex;
    for ( i = 0; i < 8; i++ )
    {
        ss << dis( gen );
    }
    ss << "-";
    for ( i = 0; i < 4; i++ )
    {
        ss << dis( gen );
    }
    ss << "-4";
    for ( i = 0; i < 3; i++ )
    {
        ss << dis( gen );
    }
    ss << "-";
    ss << dis2( gen );
    for ( i = 0; i < 3; i++ )
    {
        ss << dis( gen );
    }
    ss << "-";
    for ( i = 0; i < 12; i++ )
    {
        ss << dis( gen );
    };
    return ss.str();
}

std::string getCurrentTimeString()
{
    auto now = std::chrono::system_clock::now();
    auto itt = std::chrono::system_clock::to_time_t( now );
    std::ostringstream ss;

#ifdef _WIN32
    struct tm buf;
    gmtime_s( &buf, &itt );
    ss << std::put_time( &buf, "%FT%TZ" );
#else
    ss << std::put_time( gmtime( &itt ), "%FT%TZ" );
#endif

    return ss.str();
}
