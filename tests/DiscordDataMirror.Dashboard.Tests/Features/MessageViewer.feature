Feature: Message Viewer
    As a user
    I want to view messages in a Discord channel
    So that I can read archived conversations

Background:
    Given the dashboard application is running

@messages @smoke
Scenario: Message viewer displays channel messages
    Given I am viewing a channel with messages
    Then I should see a list of message cards
    And each message should display the author name
    And each message should display the message content
    And each message should display a timestamp

@messages
Scenario: Messages are grouped by author
    Given I am viewing a channel with messages
    Then consecutive messages from the same author should be grouped together

@messages @navigation
Scenario: Breadcrumb navigation works
    Given I am viewing a channel
    Then I should see breadcrumb navigation
    When I click on the guild name in the breadcrumb
    Then I should be navigated to the guild overview

@messages @pagination
Scenario: Loading older messages works
    Given I am viewing a channel with many messages
    When I click "Load older messages"
    Then more messages should be displayed

@messages @pagination
Scenario: Jumping to oldest messages works
    Given I am viewing a channel
    When I click the "Oldest" button
    Then I should see the oldest messages in the channel

@messages @pagination
Scenario: Jumping to newest messages works
    Given I am viewing a channel at the oldest messages
    When I click the "Newest" button
    Then I should see the newest messages in the channel

@messages @search
Scenario: Searching messages filters results
    Given I am viewing a channel
    When I enter a search term in the search field
    And I press Enter
    Then only messages containing the search term should be displayed

@messages @navigation
Scenario: Navigating to a different channel updates messages
    Given I am viewing a channel
    When I navigate to a different channel using the URL
    Then the messages should update to the new channel
    And the page title should update to the new channel name
