Feature: Setup users partner level API
  As a QA engineer
  I want to describe the users partner level endpoint in Gherkin
  So the expected behaviour is easy to verify and share

  Background:
    Given the API base URL is configured

  @api @usersPartnerLevel @happyPath
  Scenario: Retrieve users partner level data
    When I send a GET request to "/api/setup/usersPartnerLevel?partnerId=178764"
    Then the response status code should be 200
    And the response body should not be empty
    And the response body should be valid json

  @api @usersPartnerLevel @structure
  Scenario: Users partner level endpoint structure is correct
    When I build the users partner level URL for partner "178764"
    Then the users partner level request should target partner "178764"

  @api @usersPartnerLevel @headers
  Scenario: Users partner level request includes required headers
    Then the following request headers should be present:
      | Header              |
      | IsCalledByJob       |
      | X-VaxHub-Identifier |
      | traceparent         |
      | MobileData          |
      | UserSessionId       |
      | MessageSource       |
      | User-Agent          |

