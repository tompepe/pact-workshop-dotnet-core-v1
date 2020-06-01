.[] |  {      
    methodPath:
        .message | split("No interaction found for ")[1] | split(" "),     
    body:
    { 
        operationName:
            .interaction_diffs | map(.body.operationName) | add | .ACTUAL,      
        variables:
            .interaction_diffs | map(.body.variables | to_entries | map(select(.value.ACTUAL != "<key not found>")) | map({(.key): .value.ACTUAL})) | add | add,
        query:
            .interaction_diffs | map(.body.query) | add | del(.EXPECTED) | .ACTUAL,
    }
} | {
    method:
        .methodPath[0], 
    path:
        .methodPath[1], 
    body
}
