Feature: Application Navigation
    As a user
    I want to navigate between different pages of the application
    So that I can access all features of the Discord Data Mirror

    Background:
        Given the application is running with test data

    Scenario: Navigate from home to guild overview
        Given I am on the dashboard home page
        When I click on the server card "Test Server Alpha"
        Then I should be on the guild overview page
        And the URL should contain "/guild/123456789012345678"

    Scenario: Navigate from guild overview to channel browser
        Given I am on the guild overview page for guild "123456789012345678"
        When I click the "View All" button for channels
        Then I should be on the channel browser page
        And the URL should contain "/channels"

    Scenario: Navigate from guild overview to member list
        Given I am on the guild overview page for guild "123456789012345678"
        When I click the "View All" button for members
        Then I should be on the member list page
        And the URL should contain "/members"

    Scenario: Navigate from channel browser to message viewer
        Given I am on the channel browser page for guild "123456789012345678"
        And I have selected channel "general"
        When I click the "View Messages" button
        Then I should be on the message viewer page
        And the URL should contain "/channel/"

    Scenario: Navigate using breadcrumbs - Home
        Given I am on the channel browser page for guild "123456789012345678"
        When I click on breadcrumb "Home"
        Then I should be on the dashboard home page

    Scenario: Navigate using breadcrumbs - Guild
        Given I am on the channel browser page for guild "123456789012345678"
        When I click on breadcrumb "Test Server Alpha"
        Then I should be on the guild overview page

    Scenario: Navigate to sync status page
        Given I am on the dashboard home page
        When I navigate to the sync status page
        Then I should see the page header "Sync Status"

    Scenario: Back button works correctly
        Given I am on the dashboard home page
        When I click on the server card "Test Server Alpha"
        And I navigate back
        Then I should be on the dashboard home page

    Scenario: Direct URL navigation to guild
        When I navigate directly to "/guild/123456789012345678"
        Then I should be on the guild overview page for "Test Server Alpha"

    Scenario: Direct URL navigation to channel browser
        When I navigate directly to "/guild/123456789012345678/channels"
        Then I should be on the channel browser page

    Scenario: Direct URL navigation to member list
        When I navigate directly to "/guild/123456789012345678/members"
        Then I should be on the member list page

    Scenario: Direct URL navigation to message viewer
        When I navigate directly to "/guild/123456789012345678/channel/111111111111111111"
        Then I should be on the message viewer page
