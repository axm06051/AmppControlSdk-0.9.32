//
// Copyright Grass Valley
//

#include "BearerToken.h"

#include <curl/curl.h>
#include <iostream>
#include <nlohmann/json.hpp>
#include <string>

using json = nlohmann::json;
using namespace std;

namespace
{
    size_t writeFunction( void* ptr, size_t size, size_t nmemb, std::string* data )
    {
        data->append( ( char* ) ptr, size * nmemb );
        return size * nmemb;
    }
}

bool getToken( const UString& in_baseUrl, const UString& in_credentials, UString& out_token, unsigned int& out_expiresIn )
{
    bool result = false;

    curl_global_init( CURL_GLOBAL_DEFAULT );
    auto curl = curl_easy_init();
    if ( curl )
    {
        std::string fullUrl = in_baseUrl + "/identity/connect/token";
        std::string postData = "grant_type=client_credentials&scope=platform";
        std::string response_string;
        std::string header_string;

        /* ask libcurl to show us the verbose output */
        //curl_easy_setopt( curl, CURLOPT_VERBOSE, 1L);

        curl_easy_setopt( curl, CURLOPT_URL, fullUrl.c_str() );
        curl_easy_setopt( curl, CURLOPT_POST, 1 );
        curl_easy_setopt( curl, CURLOPT_POSTFIELDS, postData.c_str() );
        curl_easy_setopt( curl, CURLOPT_TCP_KEEPALIVE, 1L );
        curl_easy_setopt( curl, CURLOPT_WRITEFUNCTION, writeFunction );
        curl_easy_setopt( curl, CURLOPT_WRITEDATA, &response_string );
        curl_easy_setopt( curl, CURLOPT_HEADERDATA, &header_string );
        curl_easy_setopt( curl, CURLOPT_USERAGENT, "AmppNativeApi" );

        struct curl_slist* headers = NULL;
        headers = curl_slist_append( headers, "Content-Type: application/x-www-form-urlencoded" );
        headers = curl_slist_append( headers, "Accept: application/json" );
        std::ostringstream ss;
        ss << "Authorization: Basic " << in_credentials;
        headers = curl_slist_append( headers, ss.str().c_str() );

        curl_easy_setopt( curl, CURLOPT_HTTPHEADER, headers );

        curl_easy_perform( curl );

        std::cout << "*************** CURL RESPONSE ***************" << std::endl;
        std::cout << response_string << std::endl;
        std::cout << "*********************************************" << std::endl;

        long httpCode = 999;
        int ret = curl_easy_getinfo( curl, CURLINFO_RESPONSE_CODE, &httpCode );
        if ( !ret )
        {
            if ( httpCode >= 200 && httpCode <= 299 )
            {
                json responseJson = json::parse( response_string );
                out_token = responseJson[ "access_token" ];
                out_expiresIn = responseJson[ "expires_in" ];
                result = true;
            }
            else
            {
                std::cout << "getToken() error. Returned http code: " << httpCode << std::endl;
            }
        }

        curl_easy_cleanup( curl );
        curl_global_cleanup();
        curl_slist_free_all( headers );
        curl = NULL;
    }

    return result;
}

