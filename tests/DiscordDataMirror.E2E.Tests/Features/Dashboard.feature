Feature: Dashboard
    As a user
    I want to access the Discord Data Mirror dashboard
    So that I can view synced Discord data

    Scenario: Dashboard home page loads successfully
        When I navigate to the dashboard
        Then the page title should contain "Discord Data Mirror"
        And the page should display the home content

    Scenario: Navigate to guild overview
        Given I am on the dashboard home page
        When I click on a guild in the sidebar
        Then I should see the guild overview page

    Scenario: View sync status
        Given I am on the dashboard home page
        When I navigate to the sync status page
        Then I should see the sync status information

    Scenario: Dashboard shows connection status
        When I navigate to the dashboard
        Then I should see the connection status indicator
