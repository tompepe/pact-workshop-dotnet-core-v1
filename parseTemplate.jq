{
    request:[
            .,
            {
                matchingRules:{
                    "$.body.operationName": {
                        match: "regex",
                        regex: ".*"
                    },
                    "$.body.query": {
                        match: "regex",
                        regex: (
                            "[/" + 
                            .body.query 
                                | gsub("[\\n\\t]";".*") 
                                | gsub("[(]";".") 
                                | gsub("[$]";".") 
                                | gsub("[..]";".*") 
                                | gsub("[**]";"*")
                            + "/m]"
                        )
                    }
                }
            }
        ] | add,
    response:
        { 
    status:
        200, 
    headers: { 
        "Content-Type": "application/json; charset=utf-8" 
    }, 
    body:
        {
            data:{}
        }
    }
}
