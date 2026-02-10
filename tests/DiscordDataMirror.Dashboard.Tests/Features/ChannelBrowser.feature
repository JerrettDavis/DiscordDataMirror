Feature: Channel Browser
    As a user
    I want to browse channels in a Discord server
    So that I can find and view specific channel messages

Background:
    Given the dashboard application is running
    And I am on a guild overview page

@channels @smoke
Scenario: Channel browser displays categories and channels
    When I navigate to the channel browser
    Then I should see a list of channel categories
    And I should see channels within categories

@channels
Scenario: Clicking a channel shows channel details
    Given I am on the channel browser
    When I click on a channel
    Then I should see the channel details panel
    And I should see the "View Messages" button

@channels @navigation
Scenario: Clicking "View Messages" navigates to message viewer
    Given I am on the channel browser
    And I have selected a channel
    When I click the "View Messages" button
    Then I should be navigated to the message viewer
    And the URL should contain "/channel/"

@channels @navigation
Scenario: Navigating between channels updates the view
    Given I am on the channel browser
    When I click on the first channel
    And I click on a different channel
    Then the channel details should update to the new channel

@channels
Scenario: Channel browser shows message counts
    When I navigate to the channel browser
    Then each channel should display a message count badge
