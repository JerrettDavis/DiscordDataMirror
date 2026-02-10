Feature: Dashboard Home Page
    As a user
    I want to access the Discord Data Mirror dashboard
    So that I can view an overview of synced Discord data

    Background:
        Given the application is running with test data

    Scenario: Dashboard home page loads successfully
        When I navigate to the dashboard
        Then the page title should contain "Discord Data Mirror"
        And I should see the page header "Discord Data Mirror"
        And I should see the subtitle "Your Discord server archive and analytics dashboard"

    Scenario: Dashboard displays statistics cards
        When I navigate to the dashboard
        Then I should see a stats card showing "Servers"
        And I should see a stats card showing "Channels"
        And I should see a stats card showing "Messages"
        And I should see a stats card showing "Users"

    Scenario: Dashboard displays synced servers
        When I navigate to the dashboard
        Then I should see the heading "Synced Servers"
        And I should see a server card for "Test Server Alpha"
        And I should see a server card for "Test Server Beta"

    Scenario: Server card displays sync status
        When I navigate to the dashboard
        Then the server card "Test Server Alpha" should show sync status

    Scenario: Navigate to guild from server card
        Given I am on the dashboard home page
        When I click on the server card "Test Server Alpha"
        Then I should be on the guild overview page for "Test Server Alpha"
        And the URL should contain "/guild/"
