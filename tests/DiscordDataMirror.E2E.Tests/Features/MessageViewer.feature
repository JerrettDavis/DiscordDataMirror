Feature: Message Viewer Page
    As a user
    I want to view messages in a Discord channel
    So that I can read archived conversations

    Background:
        Given the application is running with test data

    Scenario: Message viewer page loads successfully
        When I navigate to the message viewer for channel "111111111111111111" in guild "123456789012345678"
        Then the page title should contain "general"
        And I should see the channel name "#general"

    Scenario: Message viewer displays breadcrumbs
        When I navigate to the message viewer for channel "111111111111111111" in guild "123456789012345678"
        Then I should see breadcrumb "Home"
        And I should see breadcrumb "Test Server Alpha"
        And I should see breadcrumb "Channels"

    Scenario: Message viewer displays messages
        When I navigate to the message viewer for channel "111111111111111111" in guild "123456789012345678"
        Then I should see messages in the channel
        And I should see message content containing "Hello everyone"

    Scenario: Messages show author information
        When I navigate to the message viewer for channel "111111111111111111" in guild "123456789012345678"
        Then I should see message author names
        And I should see message timestamps

    Scenario: Message viewer shows total message count
        When I navigate to the message viewer for channel "111111111111111111" in guild "123456789012345678"
        Then I should see the total message count for the channel

    Scenario: Message viewer shows date range
        When I navigate to the message viewer for channel "111111111111111111" in guild "123456789012345678"
        Then I should see the message date range

    Scenario: Load older messages
        Given I am on the message viewer for channel "111111111111111111" in guild "123456789012345678"
        And there are more older messages available
        When I click "Load older messages"
        Then older messages should be loaded

    Scenario: Jump to oldest messages
        Given I am on the message viewer for channel "111111111111111111" in guild "123456789012345678"
        When I click the "Oldest" button
        Then I should see the oldest messages in the channel

    Scenario: Jump to newest messages
        Given I am on the message viewer for channel "111111111111111111" in guild "123456789012345678"
        When I click the "Newest" button
        Then I should see the newest messages in the channel

    Scenario: Search messages
        Given I am on the message viewer for channel "111111111111111111" in guild "123456789012345678"
        When I search for "Hello"
        Then I should see messages containing "Hello"

    Scenario: Message groups are displayed correctly
        When I navigate to the message viewer for channel "111111111111111111" in guild "123456789012345678"
        Then consecutive messages from the same author should be grouped
