Feature: Patients insurance by state API
  As a QA engineer
  I want to cover the insurance-by-state endpoint with Gherkin scenarios
  So the behaviour stays transparent and testable

  Background:
    Given the API base URL is configured

  @api @insuranceByState @happyPath
  Scenario: Retrieve insurance data for state FL
    When I send a GET request to "/api/patients/insurance/bystate/FL?contractedOnly=false"
    Then the response status code should be 200
    And the response body should not be empty
    And the response body should be valid json

  @api @insuranceByState @structure
  Scenario: Insurance by state endpoint structure is correct
    When I build the insurance by state URL for state "FL" with contractedOnly "false"
    Then the insurance by state request should target state "FL"
    And the insurance by state query should be "?contractedOnly=false"
    And the insurance by state state code "FL" should be valid

  @api @insuranceByState @headers
  Scenario: Insurance by state request includes required headers
    Then the following request headers should be present:
      | Header              |
      | IsCalledByJob       |
      | X-VaxHub-Identifier |
      | traceparent         |
      | MobileData          |
      | UserSessionId       |
      | MessageSource       |
      | User-Agent          |

