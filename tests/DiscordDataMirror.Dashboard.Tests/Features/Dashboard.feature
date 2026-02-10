Feature: Dashboard
    As a user
    I want to view the Discord Data Mirror dashboard
    So that I can see an overview of synced servers

Background:
    Given the dashboard application is running

@smoke
Scenario: Dashboard displays server statistics
    When I navigate to the dashboard
    Then I should see statistics cards for Servers, Channels, Messages, and Users

@smoke
Scenario: Dashboard displays synced servers
    When I navigate to the dashboard
    Then I should see a "Synced Servers" section
    And I should see at least one server card

@navigation
Scenario: Clicking a server navigates to guild overview
    Given I am on the dashboard
    When I click on a server card
    Then I should be navigated to the guild overview page
    And the URL should contain "/guild/"

@navigation
Scenario: Dashboard has responsive navigation
    When I navigate to the dashboard
    Then I should see the main navigation elements
