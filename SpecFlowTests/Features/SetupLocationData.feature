Feature: Setup location data API
  As a QA engineer
  I want to cover the location data endpoint using Gherkin
  So its behaviour is executable and documented

  Background:
    Given the API base URL is configured

  @api @locationData @happyPath
  Scenario: Retrieve location data for clinic 89534
    When I send a GET request to "/api/setup/LocationData?clinicId=89534"
    Then the response status code should be 200
    And the response body should not be empty
    And the response body should be valid json

  @api @locationData @structure
  Scenario: Location data endpoint structure is correct
    When I build the location data URL for clinic "89534"
    Then the location data request should target clinic "89534"

  @api @locationData @headers
  Scenario: Location data request includes required headers
    Then the following request headers should be present:
      | Header              |
      | IsCalledByJob       |
      | X-VaxHub-Identifier |
      | traceparent         |
      | MobileData          |
      | UserSessionId       |
      | MessageSource       |
      | User-Agent          |

