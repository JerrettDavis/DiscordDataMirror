Feature: Sync Status Page
    As a user
    I want to monitor synchronization status of Discord data
    So that I can track sync progress and troubleshoot issues

    Background:
        Given the application is running with test data

    Scenario: Sync status page loads successfully
        When I navigate to the sync status page
        Then the page title should contain "Sync Status"
        And I should see the page header "Sync Status"
        And I should see the subtitle "Monitor and manage data synchronization"

    Scenario: Sync status displays overview statistics
        When I navigate to the sync status page
        Then I should see a stats card showing "In Progress"
        And I should see a stats card showing "Completed"
        And I should see a stats card showing "Failed"
        And I should see a stats card showing "Idle"

    Scenario: Sync status displays filter tabs
        When I navigate to the sync status page
        Then I should see tab "All"
        And I should see tab "In Progress"
        And I should see tab "Completed"
        And I should see tab "Failed"

    Scenario: Filter sync states by status
        Given I am on the sync status page
        When I click on tab "Completed"
        Then I should only see sync states with status "Completed"

    Scenario: Sync status groups by guild
        When I navigate to the sync status page
        Then I should see sync states grouped by "Test Server Alpha"
        And I should see sync states grouped by "Test Server Beta"

    Scenario: Sync state shows entity type
        When I navigate to the sync status page
        Then I should see sync state entity type "Guild"
        And I should see sync state entity type "Channel"

    Scenario: Sync state shows last synced time
        When I navigate to the sync status page
        Then completed sync states should show "Last Synced" time

    Scenario: Refresh sync data
        Given I am on the sync status page
        When I click the "Refresh" button
        Then the sync data should be refreshed

    Scenario: Trigger guild sync
        Given I am on the sync status page
        When I click "Sync Now" for guild "Test Server Alpha"
        Then a sync request should be triggered

    Scenario: Sync all guilds dialog
        Given I am on the sync status page
        When I click the "Sync All Guilds" button
        Then I should see a confirmation dialog

    Scenario: Recent activity timeline
        When I navigate to the sync status page
        Then I should see the heading "Recent Activity"
        And I should see recent sync activity in a timeline

    Scenario: In-progress sync shows progress indicator
        When I navigate to the sync status page
        Then in-progress sync states should show a loading indicator

    Scenario: Failed sync shows error message
        Given there is a failed sync state
        When I navigate to the sync status page
        Then I should see an error indicator for failed syncs

    Scenario: Navigate to guild from sync status
        Given I am on the sync status page
        When I click "View Server" for "Test Server Alpha"
        Then I should be navigated to the guild overview page
