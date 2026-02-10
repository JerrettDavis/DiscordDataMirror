Feature: Member List Page
    As a user
    I want to view and search members in a Discord server
    So that I can find specific users and see their activity

    Background:
        Given the application is running with test data

    Scenario: Member list page loads successfully
        When I navigate to the member list page for guild "123456789012345678"
        Then the page title should contain "Members"
        And I should see the heading "Members"
        And I should see the total member count

    Scenario: Member list displays breadcrumbs
        When I navigate to the member list page for guild "123456789012345678"
        Then I should see breadcrumb "Home"
        And I should see breadcrumb "Test Server Alpha"
        And I should see breadcrumb "Members"

    Scenario: Member list displays member table
        When I navigate to the member list page for guild "123456789012345678"
        Then I should see a table with columns "Member", "Joined", "Messages", "Roles"
        And I should see member "TestUser1" in the list
        And I should see member "TestUser2" in the list

    Scenario: Member displays nickname when available
        When I navigate to the member list page for guild "123456789012345678"
        Then I should see member "Alpha Tester" in the list
        And I should see member "Beta Tester" in the list

    Scenario: Bot users are identified
        When I navigate to the member list page for guild "123456789012345678"
        Then I should see a "BOT" tag for bot users

    Scenario: Members display their roles
        When I navigate to the member list page for guild "123456789012345678"
        Then member "AdminUser" should display role "Admin"
        And member "TestUser2" should display role "Moderator"

    Scenario: Search members by name
        Given I am on the member list page for guild "123456789012345678"
        When I search for member "TestUser1"
        Then I should see member "TestUser1" in the results
        And I should not see member "TestUser2" in the results

    Scenario: Search members by nickname
        Given I am on the member list page for guild "123456789012345678"
        When I search for member "Alpha"
        Then I should see member "Alpha Tester" in the results

    Scenario: Filter members by role
        Given I am on the member list page for guild "123456789012345678"
        When I filter by role "Admin"
        Then I should only see members with the "Admin" role

    Scenario: Sort members by join date
        Given I am on the member list page for guild "123456789012345678"
        When I sort by "Joined Date"
        Then members should be sorted by their join date

    Scenario: Sort members by name
        Given I am on the member list page for guild "123456789012345678"
        When I sort by "Name"
        Then members should be sorted alphabetically by name

    Scenario: Sort members by message count
        Given I am on the member list page for guild "123456789012345678"
        When I sort by "Messages"
        Then members should be sorted by their message count

    Scenario: Member shows message count
        When I navigate to the member list page for guild "123456789012345678"
        Then each member should display their message count
