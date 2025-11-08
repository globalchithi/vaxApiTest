Feature: Patients appointment sync API
  As a VaxCare QA engineer
  I want to document the appointment sync endpoint with Gherkin
  So that its expectations are executable and clear

  Background:
    Given the API base URL is configured

  @api @appointmentSync @happyPath
  Scenario: Retrieve appointment sync data
    When I send a GET request to "/api/patients/appointment/sync?clinicId=89534&date=2025-10-22&version=2.0"
    Then the response status code should be 200
    And the response header "Content-Type" should contain "application/json"
    And the response body should not be empty
    And the response body should be valid json

  @api @appointmentSync @queryParams
  Scenario: Appointment sync query parameters are well formed
    When I build the appointment sync URL with clinicId "89534" date "2025-10-22" version "2.0"
    Then the request URL should have absolute path "/api/patients/appointment/sync"
    And the appointment sync query string should include clinicId "89534" date "2025-10-22" version "2.0"
    And the appointment sync date "2025-10-22" should match yyyy-MM-dd format
    And the appointment sync version "2.0" should match X.Y format
    And the appointment sync clinicId "89534" should be a positive integer

  @api @appointmentSync @dateFormat
  Scenario Outline: Appointment sync accepts valid date formats
    Then the appointment sync date "<date>" should match yyyy-MM-dd format

    Examples:
      | date        |
      | 2025-10-22  |
      | 2024-12-31  |
      | 2023-01-01  |
      | 2026-06-15  |

  @api @appointmentSync @dateFormat @negative
  Scenario Outline: Appointment sync rejects invalid date formats
    Then the appointment sync date "<date>" should be invalid

    Examples:
      | date         |
      | 22-10-2025   |
      | 2025/10/22   |
      | 25-10-22     |
      | invalid-date |

  @api @appointmentSync @versionFormat
  Scenario Outline: Appointment sync version format validation
    Then the appointment sync version "<version>" should match X.Y format

    Examples:
      | version |
      | 2.0     |
      | 1.5     |
      | 3.14    |
      | 10.0    |
      | 0.1     |

  @api @appointmentSync @versionFormat @negative
  Scenario Outline: Appointment sync rejects invalid version formats
    Then the appointment sync version "<version>" should be invalid

    Examples:
      | version   |
      | 2         |
      | v2.0      |
      | 2.0-beta  |
      | invalid   |

  @api @appointmentSync @clinicIdFormat
  Scenario Outline: Appointment sync clinic id format validation
    Then the appointment sync clinicId "<clinicId>" should be a positive integer

    Examples:
      | clinicId |
      | 89534    |
      | 1        |
      | 999999   |
      | 12345    |

  @api @appointmentSync @clinicIdFormat @negative
  Scenario Outline: Appointment sync rejects invalid clinic ids
    Then the appointment sync clinicId "<clinicId>" should be invalid

    Examples:
      | clinicId  |
      | abc123    |
      | 89-534    |
      | 89.534    |
      | 89 534    |
      |           |
      | -123      |

  @api @appointmentSync @logging
  Scenario: Appointment sync response logging demonstration
    Then I demonstrate appointment sync response logging

