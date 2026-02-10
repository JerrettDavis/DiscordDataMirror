Feature: Guild Overview Page
    As a user
    I want to view detailed information about a Discord server
    So that I can see server stats, channels, members, and roles

    Background:
        Given the application is running with test data

    Scenario: Guild overview page loads successfully
        When I navigate to the guild overview page for guild "123456789012345678"
        Then the page title should contain "Test Server Alpha"
        And I should see the guild name "Test Server Alpha"

    Scenario: Guild overview displays statistics
        When I navigate to the guild overview page for guild "123456789012345678"
        Then I should see a stats card showing "Channels"
        And I should see a stats card showing "Members"
        And I should see a stats card showing "Messages"
        And I should see a stats card showing "Roles"

    Scenario: Guild overview displays sync status section
        When I navigate to the guild overview page for guild "123456789012345678"
        Then I should see the heading "Sync Status"
        And I should see "Last Synced"
        And I should see "Status"

    Scenario: Guild overview displays recent channels
        When I navigate to the guild overview page for guild "123456789012345678"
        Then I should see the heading "Channels"
        And I should see a "View All" button for channels
        And I should see channel "general" in the list
        And I should see channel "announcements" in the list

    Scenario: Guild overview displays top members
        When I navigate to the guild overview page for guild "123456789012345678"
        Then I should see the heading "Top Members"
        And I should see a "View All" button for members

    Scenario: Guild overview displays roles
        When I navigate to the guild overview page for guild "123456789012345678"
        Then I should see the heading "Roles"
        And I should see role "Admin" in the roles section
        And I should see role "Moderator" in the roles section
        And I should see role "Member" in the roles section

    Scenario: Navigate to channel from guild overview
        Given I am on the guild overview page for guild "123456789012345678"
        When I click on channel "general"
        Then the URL should contain "/channel/"

    Scenario: Navigate to all channels from guild overview
        Given I am on the guild overview page for guild "123456789012345678"
        When I click the "View All" button for channels
        Then the URL should contain "/channels"

    Scenario: Navigate to all members from guild overview
        Given I am on the guild overview page for guild "123456789012345678"
        When I click the "View All" button for members
        Then the URL should contain "/members"
