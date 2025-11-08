Feature: Patients clinic API
  As a quality engineer
  I want to describe the patients clinic endpoint behaviour in Gherkin
  So that stakeholders can understand the expectations

  Background:
    Given the API base URL is configured

  @api @patientsClinic @happyPath
  Scenario: Retrieve patients clinic data
    When I send a GET request to "/api/patients/clinic"
    Then the response status code should be 200
    And the response body should not be empty
    And the response body should be valid json

  @api @patientsClinic @structure
  Scenario: Patients clinic endpoint path matches configuration
    When I build the request URL for "/api/patients/clinic"
    Then the request URL should have absolute path "/api/patients/clinic"

  @api @patientsClinic @headers
  Scenario: Patients clinic request includes required headers
    Then the following request headers should be present:
      | Header              |
      | IsCalledByJob       |
      | X-VaxHub-Identifier |
      | traceparent         |
      | MobileData          |
      | UserSessionId       |
      | MessageSource       |
      | User-Agent          |

