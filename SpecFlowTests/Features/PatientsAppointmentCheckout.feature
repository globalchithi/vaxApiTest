Feature: Patients appointment checkout
  As a VaxCare QA engineer
  I want to describe appointment checkout flows in Gherkin
  So the business behaviour stays transparent

  Background:
    Given the API base URL is configured

  @api @checkout @happyPath
  Scenario: Checkout completes for a risk-free patient with a single vaccine
    Given a "RiskFree" patient appointment exists
    And a checkout payload for single vaccine with payment mode "InsurancePay"
    When I submit the checkout request expecting success
    Then the checkout response should be valid json

  @api @checkout @validation
  Scenario: Checkout validates required administered vaccines
    Given a "RiskFree" patient appointment exists
    And a checkout payload missing administered vaccines
    When I submit the checkout request
    Then the checkout request should succeed

  @api @checkout @invalid
  Scenario: Checkout fails for an invalid appointment id
    Given an appointment id of "999999"
    And a checkout payload for single vaccine with payment mode "InsurancePay"
    When I submit the checkout request
    Then the checkout request status should be "NotFound"

  @api @checkout @multi
  Scenario: Checkout completes with multiple vaccines
    Given a "RiskFree" patient appointment exists
    And a checkout payload with multiple vaccines
    When I submit the checkout request expecting success
    Then the checkout response should be valid json

  @api @checkout @selfpay
  Scenario: Checkout completes for a self-pay patient with credit card details
    Given a "SelfPay" patient appointment exists
    And a checkout payload for self-pay with credit card details
    When I submit the checkout request expecting success
    Then the checkout response should be valid json

  @api @checkout @vfc
  Scenario: Checkout completes for a VFC patient
    Given a "VFC" patient appointment exists
    And a checkout payload for VFC patient
    When I submit the checkout request expecting success
    Then the checkout response should be valid json

  @api @checkout @dose
  Scenario: Checkout handles different dose series
    Given a "RiskFree" patient appointment exists
    And a checkout payload with mixed dose series
    When I submit the checkout request expecting success
    Then the checkout response should be valid json

  @api @checkout @empty
  Scenario: Checkout completes with empty vaccine list
    Given a "RiskFree" patient appointment exists
    And a checkout payload with no vaccines
    When I submit the checkout request
    Then the checkout request should succeed

