Feature: Patients staffer shot administrators API
  As a QA engineer
  I want Gherkin coverage for the staffer shot administrators endpoint
  So that its behaviour stays documented and executable

  Background:
    Given the API base URL is configured

  @api @stafferShotAdmins @happyPath
  Scenario: Retrieve staffer shot administrators data
    When I send a GET request to "/api/patients/staffer/shotadministrators"
    Then the response status code should be 200
    And the response body should not be empty
    And the response body should be valid json

  @api @stafferShotAdmins @structure
  Scenario: Staffer shot administrators endpoint path matches configuration
    When I build the request URL for "/api/patients/staffer/shotadministrators"
    Then the request URL should have absolute path "/api/patients/staffer/shotadministrators"

  @api @stafferShotAdmins @headers
  Scenario: Staffer shot administrators request includes required headers
    Then the following request headers should be present:
      | Header              |
      | IsCalledByJob       |
      | X-VaxHub-Identifier |
      | traceparent         |
      | MobileData          |
      | UserSessionId       |
      | MessageSource       |
      | User-Agent          |

