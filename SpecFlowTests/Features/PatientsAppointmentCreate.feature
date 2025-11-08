Feature: Patients appointment creation
  As a VaxCare QA engineer
  I want to express appointment creation behaviour in Gherkin
  So that we can validate core flows collaboratively

  Background:
    Given the API base URL is configured

  @api @appointmentCreate @happyPath
  Scenario: Create appointment returns an appointment id
    Given a unique appointment creation payload
    When I submit the appointment creation request
    Then the response status code should be 200
    And the response body should not be empty
    And an appointment id should be returned

  @api @appointmentCreate @structure
  Scenario: Appointment creation endpoint structure is correct
    When I build the request URL for "/api/patients/appointment"
    Then the request URL should have absolute path "/api/patients/appointment"

  @api @appointmentCreate @uniqueName
  Scenario: Appointment creation handles unique patient names
    Given a unique appointment creation payload
    When I submit the appointment creation request
    Then the response status code should be 200
    And the response body should not be empty
    And the appointment creation response should not contain "duplicate"
    And the appointment creation response should not contain "already exists"

