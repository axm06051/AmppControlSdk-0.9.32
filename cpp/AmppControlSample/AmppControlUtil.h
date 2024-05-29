//
// Copyright Grass Valley
//

#ifndef AMPPCONTROL_H_
#define AMPPCONTROL_H_

typedef std::string UString;

// Generates a REST API call to retrieve the list of applications registered to
// Ampp Control.
// Please refer to: https://{platform}/ampp/control/swagger/index.html
bool getAmppControlApplications( const UString& in_baseUrl,
    const UString& in_credentials, UString& out_applications );

// Generates a REST API call to retrieve the list of workload IDs associated with
// a specific application.
// Please refer to: https://{platform}/ampp/control/swagger/index.html
bool getAmppControlWorkloads( const UString& in_baseUrl,
    const UString& in_credentials, const UString& in_application,
    UString& out_workloads );

#endif /* AMPPCONTROL_H_ */
