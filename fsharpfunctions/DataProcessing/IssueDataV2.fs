namespace FSharpFunctions
open System
open System.Collections.Generic


//new types
type SubService = {Name: string; Count: int}
type Service = {Name: string; Count: int; SubServiceCounts: option<list<SubService>>}

type IssueDataV2 = {
    Id: string
    Source: Source
    EntryType: EntryType //we know what possible values are
    Timestamp: DateTime
    TotalOpenIssues: int
    MissingTags: int
    CountByPriority: option<Map<string,int>> //immutable key-value store
    CountByService: option<list<Service>>  // union type allow both old and new data types.
}