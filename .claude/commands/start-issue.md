A custom command to fetch GitHub issues, analyze implementation requirements, create TODO lists, and start implementation.


## Usage

```bash
# Specify by issue number
start-issue 123

# Specify by GitHub URL
start-issue https://github.com/owner/repo/issues/123
```

## Features

1. **GitHub Issue Fetching**: Retrieve issue information using GitHub CLI
2. **Implementation Analysis**: Extract implementation requirements from issue content
3. **TODO List Generation**: Structure changes into file-based TODO items
4. **Implementation Start**: Begin implementation based on generated TODO list

## Supported Formats

- **Issue Number**: `123` (fetches issue from current repository)
- **GitHub URL**: `https://github.com/owner/repo/issues/123`
- **Short URL**: `gh://owner/repo/issues/123`

## Execution Flow

1. Parse issue number or URL from arguments
2. Fetch issue information using `gh api` command
3. Analyze issue content and labels for implementation requirements
4. Identify related files
5. Generate prioritized TODO list
6. Confirm implementation start

## Implementation Logic

### Issue Information Fetching
- Use GitHub CLI (`gh api`) to retrieve issue details
- Parse issue body, labels, assignments, and other metadata

### Implementation Analysis
- Extract technical requirements from issue content
- Identify mentions of file names or class names
- Categorize features to be implemented

### TODO List Generation
- Structure each change as file-based TODO items
- Set priorities based on dependencies
- Integrate with existing TodoWrite functionality

### Implementation Start
- Display generated TODO list
- Start implementation after user confirmation
- Execute each TODO sequentially

## Error Handling

- GitHub CLI not installed
- Authentication errors
- Non-existent issue numbers
- Invalid URL formats
- Network errors

## Dependencies

- GitHub CLI (`gh`) v2.0+
- Proper GitHub access permissions
- Current directory must be a Git repository

## Usage Examples

```bash
# Process issue #42 from current repository
start-issue 42

# Process issue from another repository
start-issue https://github.com/microsoft/vscode/issues/12345

# Example execution flow:
# 1. Issue Information Fetching: "Add dark mode support"
# 2. Implementation Analysis: UI theme system implementation
# 3. TODO List Generation:
#    - Update theme configuration files
#    - Implement dark mode CSS variables
#    - Add theme toggle component
#    - Update existing components for theme support
# 4. Implementation Start Confirmation
```

## Notes

- For large issues, manual TODO adjustment is recommended
- Existing TODO lists may be overwritten
- Always review TODO list before starting implementation

## Command Implementation

This command integrates with Claude Code's existing tools:
- `Bash` tool for GitHub CLI operations
- `TodoWrite` tool for TODO list management
- `Task` tool for code analysis and implementation
- `Read`/`Write` tools for file operations

The command follows the established pattern of analyzing requirements, creating structured plans, and executing implementation tasks systematically.