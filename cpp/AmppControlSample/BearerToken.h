//
// Copyright Grass Valley
//

#ifndef BEARER_TOKEN_H_
#define BEARER_TOKEN_H_

#include <sstream>
#include <string>

typedef std::string UString;

/**
* Get a token for a set of credentials
* \return true if the call succeeded, false otherwise
*/
bool getToken( const UString& in_baseUrl, const UString& in_credentials, UString& out_token, unsigned int& out_expiresIn );

#endif /* BEARER_TOKEN_H_ */
