Feature: Patients staffer providers API
  As a QA engineer
  I want to cover staffer providers endpoint behaviour in Gherkin
  So the expected responses and structure stay visible to stakeholders

  Background:
    Given the API base URL is configured

  @api @stafferProviders @happyPath
  Scenario: Retrieve staffer providers
    When I send a GET request to "/api/patients/staffer/providers"
    Then the response status code should be 200
    And the response body should not be empty
    And the response body should be valid json

  @api @stafferProviders @structure
  Scenario: Staffer providers endpoint structure is correct
    When I build the request URL for "/api/patients/staffer/providers"
    Then the request URL should have absolute path "/api/patients/staffer/providers"

  @api @stafferProviders @headers
  Scenario: Staffer providers request includes required headers
    Then the following request headers should be present:
      | Header              |
      | IsCalledByJob       |
      | X-VaxHub-Identifier |
      | traceparent         |
      | MobileData          |
      | UserSessionId       |
      | MessageSource       |
      | User-Agent          |

