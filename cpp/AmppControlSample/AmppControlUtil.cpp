//
// Copyright Grass Valley
//

#include "BearerToken.h"

#include <curl/curl.h>
#include <iostream>
#include <string>

using namespace std;

namespace
{
    size_t writeFunction( void* ptr, size_t size, size_t nmemb, std::string* data )
    {
        data->append( ( char* ) ptr, size * nmemb );
        return size * nmemb;
    }
}


// Refer to: https://{platform}/ampp/control/swagger/index.html
bool getAmppControlApplications( const UString& in_baseUrl, const UString& in_credentials, UString& out_applications )
{
    bool result = false;

    unsigned int expiresIn = 0;
    std::string bearer_token;

    if ( !getToken( in_baseUrl, in_credentials, bearer_token, expiresIn ) )
    {
        std::cout << "Could not retrieve bearer token." << std::endl;
        return false;
    }

    curl_global_init( CURL_GLOBAL_DEFAULT );
    auto curl = curl_easy_init();
    if ( curl )
    {
        std::string fullUrl = in_baseUrl + "/ampp/control/api/v1/control/application/references";
        std::string response_string;
        std::string header_string;

        /* ask libcurl to show us the verbose output */
        //curl_easy_setopt( curl, CURLOPT_VERBOSE, 1L );

        curl_easy_setopt( curl, CURLOPT_URL, fullUrl.c_str() );
        curl_easy_setopt( curl, CURLOPT_HTTPGET, 1 );
        curl_easy_setopt( curl, CURLOPT_TCP_KEEPALIVE, 10L );
        curl_easy_setopt( curl, CURLOPT_WRITEFUNCTION, writeFunction );
        curl_easy_setopt( curl, CURLOPT_WRITEDATA, &response_string );
        curl_easy_setopt( curl, CURLOPT_HEADERDATA, &header_string );
        curl_easy_setopt( curl, CURLOPT_USERAGENT, "AmppNativeApi" );
        //curl_easy_setopt( curl, CURLOPT_XOAUTH2_BEARER, bearer_token.c_str() );
        //curl_easy_setopt( curl, CURLOPT_HTTPAUTH, CURLAUTH_BEARER );

        std::string bearerHeaderString = "Authorization: Bearer " + bearer_token;

        struct curl_slist* headers = NULL;
        headers = curl_slist_append( headers, "Content-Type: application/json" );
        headers = curl_slist_append( headers, "Accept: application/json" );
        headers = curl_slist_append( headers, bearerHeaderString.c_str() );

        curl_easy_setopt( curl, CURLOPT_HTTPHEADER, headers );

        curl_easy_perform( curl );

        long httpCode = 999;
        int ret = curl_easy_getinfo( curl, CURLINFO_RESPONSE_CODE, &httpCode );
        if ( !ret )
        {
            if ( httpCode >= 200 && httpCode <= 299 )
            {
                out_applications = response_string;
                result = true;
            }
            else
            {
                std::cout << "Get Ampp Applications error. Returned http code: " << httpCode << std::endl;
            }
        }

        curl_easy_cleanup( curl );
        curl_global_cleanup();
        curl_slist_free_all( headers );
        curl = NULL;
    }

    return result;
}


// Refer to: https://{platform}/ampp/control/swagger/index.html
bool getAmppControlWorkloads( const UString& in_baseUrl,
    const UString& in_credentials, const UString& in_application,
    UString& out_workloads )
{
    bool result = false;

    unsigned int expiresIn = 0;
    std::string bearer_token;

    if ( !getToken( in_baseUrl, in_credentials, bearer_token, expiresIn ) )
    {
        std::cout << "Could not retrieve bearer token." << std::endl;
        return false;
    }

    curl_global_init( CURL_GLOBAL_DEFAULT );
    auto curl = curl_easy_init();
    if ( curl )
    {
        std::string fullUrl = in_baseUrl + "/ampp/control/api/v1/control/application/" + in_application + "/workloads";
        std::string response_string;
        std::string header_string;

        /* ask libcurl to show us the verbose output */
        // curl_easy_setopt(curl, CURLOPT_VERBOSE, 1L);

        curl_easy_setopt( curl, CURLOPT_URL, fullUrl.c_str() );
        curl_easy_setopt( curl, CURLOPT_HTTPGET, 1 );
        curl_easy_setopt( curl, CURLOPT_TCP_KEEPALIVE, 1L );
        curl_easy_setopt( curl, CURLOPT_WRITEFUNCTION, writeFunction );
        curl_easy_setopt( curl, CURLOPT_WRITEDATA, &response_string );
        curl_easy_setopt( curl, CURLOPT_HEADERDATA, &header_string );
        curl_easy_setopt( curl, CURLOPT_USERAGENT, "AmppNativeApi" );
        //curl_easy_setopt( curl, CURLOPT_XOAUTH2_BEARER, bearer_token.c_str() );
        //curl_easy_setopt( curl, CURLOPT_HTTPAUTH, CURLAUTH_BEARER );

        std::string bearerHeaderString = "Authorization: Bearer " + bearer_token;

        struct curl_slist* headers = NULL;
        headers = curl_slist_append( headers, "Content-Type: application/json" );
        headers = curl_slist_append( headers, "Accept: application/json" );
        headers = curl_slist_append( headers, bearerHeaderString.c_str() );

        curl_easy_setopt( curl, CURLOPT_HTTPHEADER, headers );

        curl_easy_perform( curl );

        long httpCode = 999;
        int ret = curl_easy_getinfo( curl, CURLINFO_RESPONSE_CODE, &httpCode );
        if ( !ret )
        {
            if ( httpCode >= 200 && httpCode <= 299 )
            {
                out_workloads = response_string;
                result = true;
            }
            else
            {
                std::cout << "Get Ampp Workloads error. Returned http code: " << httpCode << std::endl;
            }
        }

        curl_easy_cleanup( curl );
        curl_global_cleanup();
        curl_slist_free_all( headers );
        curl = NULL;
    }

    return result;
}



