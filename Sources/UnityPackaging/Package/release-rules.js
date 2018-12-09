/**
* Rules to tell `semantic-release/commit-analyzer` which commit
* messages should result in a new release and what part of the
* SemVer to bump with that release.
*
* @type {Array}
*/
module.exports = [
    {breaking: true, release: "major"},
    
    {type: "feat", release: "minor"},
    
    {revert: true, release: "patch"},
    {type: "fix", release: "patch"},
    {type: "refactor", release: "patch"},
    {type: "docs", release: "patch"}
    
    /*
    * The types listed here are valid but commented to *not*
    * automatically create a new release in case only commits with
    * these types are pushed.
    */
    /*
    {type: "test", release: "patch"},
    {type: "chore", release: "patch"}
    */
];
