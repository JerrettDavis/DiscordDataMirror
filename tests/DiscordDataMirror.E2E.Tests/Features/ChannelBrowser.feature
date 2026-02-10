Feature: Channel Browser Page
    As a user
    I want to browse and search channels in a Discord server
    So that I can find and view specific channels

    Background:
        Given the application is running with test data

    Scenario: Channel browser page loads successfully
        When I navigate to the channel browser page for guild "123456789012345678"
        Then the page title should contain "Channels"
        And I should see the heading "Channel Browser"
        And I should see a search field for channels

    Scenario: Channel browser displays breadcrumbs
        When I navigate to the channel browser page for guild "123456789012345678"
        Then I should see breadcrumb "Home"
        And I should see breadcrumb "Test Server Alpha"
        And I should see breadcrumb "Channels"

    Scenario: Channel browser displays categories with channels
        When I navigate to the channel browser page for guild "123456789012345678"
        Then I should see category "GENERAL"
        And I should see channel "general" under category "GENERAL"
        And I should see channel "announcements" under category "GENERAL"
        And I should see channel "random" under category "GENERAL"

    Scenario: Collapsing and expanding categories
        Given I am on the channel browser page for guild "123456789012345678"
        When I click on category "GENERAL" to collapse it
        Then the channels under "GENERAL" should be hidden
        When I click on category "GENERAL" to expand it
        Then I should see channel "general" under category "GENERAL"

    Scenario: Selecting a channel shows details
        Given I am on the channel browser page for guild "123456789012345678"
        When I select channel "general"
        Then I should see channel details for "general"
        And I should see the channel type "Text Channel"
        And I should see a "View Messages" button

    Scenario: Channel details show message count
        Given I am on the channel browser page for guild "123456789012345678"
        When I select channel "general"
        Then I should see the channel message count

    Scenario: Search filters channels
        Given I am on the channel browser page for guild "123456789012345678"
        When I search for "announce"
        Then I should see channel "announcements" in the results
        And I should not see channel "random" in the results

    Scenario: Navigate to message viewer from channel details
        Given I am on the channel browser page for guild "123456789012345678"
        And I have selected channel "general"
        When I click the "View Messages" button
        Then the URL should contain "/channel/"

    Scenario: Voice channels are displayed with correct icon
        Given I am on the channel browser page for guild "123456789012345678"
        Then I should see a voice channel icon for "Voice Chat"
