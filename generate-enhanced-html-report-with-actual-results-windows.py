#!/usr/bin/env python3
"""
Enhanced HTML Report Generator with Actual Results - Windows Compatible
This script parses TRX files and generates comprehensive HTML reports with actual results and concise failure reasons
Windows-compatible version with proper encoding handling
"""

import os
import sys
import xml.etree.ElementTree as ET
from datetime import datetime
import argparse
import re

def safe_print(text):
    """Safely print text that may contain Unicode characters"""
    try:
        print(text)
    except UnicodeEncodeError:
        # Fallback for Windows Command Prompt
        print(text.encode('ascii', 'replace').decode('ascii'))

def parse_trx_file(trx_file):
    """Parse TRX file and extract test results with actual results and failure reasons"""
    try:
        tree = ET.parse(trx_file)
        root = tree.getroot()
        
        # Extract test results
        test_results = []
        total_tests = 0
        passed_tests = 0
        failed_tests = 0
        skipped_tests = 0
        
        # Find all UnitTestResult elements
        for result in root.findall('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}UnitTestResult'):
            total_tests += 1
            
            test_name = result.get('testName', 'Unknown Test')
            outcome = result.get('outcome', 'Unknown')
            duration = result.get('duration', '0')
            start_time = result.get('startTime', '')
            
            # Extract class name from test name
            if '.' in test_name:
                class_parts = test_name.split('.')
                class_name = class_parts[-2] if len(class_parts) > 1 else 'Unknown'
                display_name = class_parts[-1].replace('_', ' ')
            else:
                class_name = 'Unknown'
                display_name = test_name.replace('_', ' ')
            
            # Parse duration
            try:
                # Convert duration from format like "00:00:00.1234567" to milliseconds
                if ':' in duration:
                    parts = duration.split(':')
                    hours = float(parts[0])
                    minutes = float(parts[1])
                    seconds = float(parts[2])
                    duration_ms = (hours * 3600 + minutes * 60 + seconds) * 1000
                else:
                    duration_ms = float(duration) * 1000
            except:
                duration_ms = 0
            
            # Count results (exclude skipped tests)
            if outcome == 'Passed':
                passed_tests += 1
            elif outcome == 'Failed':
                failed_tests += 1
            elif outcome == 'Skipped':
                skipped_tests += 1
                # Skip adding to test_results - exclude from report
                continue
            
            # Extract actual result and failure reason for failed tests
            actual_result = ""
            failure_reason = ""
            
            if outcome == 'Failed':
                # Look for Output/ErrorInfo first (most reliable for error details)
                output_elem = result.find('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}Output')
                if output_elem is not None:
                    # Try ErrorInfo first (contains the actual exception message)
                    error_info_elem = output_elem.find('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}ErrorInfo')
                    if error_info_elem is not None:
                        message_elem = error_info_elem.find('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}Message')
                        if message_elem is not None and message_elem.text:
                            error_text = message_elem.text
                            
                            # Look for common failure patterns in error message
                            if 'InvalidOperationException' in error_text and 'Network connectivity required' in error_text:
                                actual_result = "Network connectivity issue"
                                failure_reason = "POST operations require network connectivity - API endpoint not reachable"
                            elif 'HttpRequestException' in error_text:
                                if 'nodename nor servname provided' in error_text:
                                    actual_result = "Network connectivity issue"
                                    failure_reason = "API endpoint not reachable - DNS resolution failed"
                                elif 'Name or service not known' in error_text:
                                    actual_result = "Network connectivity issue"
                                    failure_reason = "API endpoint not reachable - hostname not found"
                                else:
                                    actual_result = "HTTP request failed"
                                    failure_reason = "Network connectivity issue"
                            elif 'TaskCanceledException' in error_text:
                                actual_result = "Request timeout"
                                failure_reason = "API endpoint timeout - server not responding"
                            elif 'TimeoutException' in error_text:
                                actual_result = "Request timeout"
                                failure_reason = "Request timed out"
                            elif 'Assertion' in error_text:
                                actual_result = "Assertion failed"
                                failure_reason = "Test assertion did not pass"
                            else:
                                # Extract first line of meaningful error
                                lines = error_text.split('\n')
                                for line in lines:
                                    line = line.strip()
                                    if line and not line.startswith('Test:') and not line.startswith('Description:'):
                                        actual_result = "Test execution failed"
                                        failure_reason = line[:100] + "..." if len(line) > 100 else line
                                        break
                    
                    # Fallback to StdOut if ErrorInfo not found
                    if not actual_result:
                        stdout_elem = output_elem.find('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}StdOut')
                        if stdout_elem is not None and stdout_elem.text:
                            # Extract concise failure reason from output
                            output_text = stdout_elem.text
                            
                            # Look for common failure patterns
                            if 'HttpRequestException' in output_text:
                                if 'nodename nor servname provided' in output_text:
                                    actual_result = "Network connectivity issue"
                                    failure_reason = "API endpoint not reachable - DNS resolution failed"
                                elif 'Name or service not known' in output_text:
                                    actual_result = "Network connectivity issue"
                                    failure_reason = "API endpoint not reachable - hostname not found"
                                else:
                                    actual_result = "HTTP request failed"
                                    failure_reason = "Network connectivity issue"
                            elif 'TaskCanceledException' in output_text:
                                actual_result = "Request timeout"
                                failure_reason = "API endpoint timeout - server not responding"
                            elif 'TimeoutException' in output_text:
                                actual_result = "Request timeout"
                                failure_reason = "Request timed out"
                            elif 'Assertion' in output_text:
                                actual_result = "Assertion failed"
                                failure_reason = "Test assertion did not pass"
                            else:
                                # Extract first line of meaningful error
                                lines = output_text.split('\n')
                                for line in lines:
                                    line = line.strip()
                                    if line and not line.startswith('Test:') and not line.startswith('Description:'):
                                        actual_result = "Test execution failed"
                                        failure_reason = line[:100] + "..." if len(line) > 100 else line
                                        break
                
                # If no specific failure reason found, use generic message
                if not actual_result:
                    actual_result = "Test execution failed"
                    failure_reason = "Test failed without specific error details"
            
            # Get test info from the test definition
            test_def = root.find(f'.//{{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}}UnitTest[@id="{result.get("testId")}"]')
            description = ""
            test_type = ""
            endpoint = ""
            expected_result = ""
            
            if test_def is not None:
                # Try to extract description from test method name or comments
                method_name = test_def.get('name', '')
                
                # Generate more accurate expected results based on test name patterns
                if 'ShouldValidate' in method_name:
                    if 'RequiredHeaders' in method_name:
                        expected_result = "All required headers validated successfully"
                    elif 'EndpointStructure' in method_name:
                        expected_result = "Endpoint structure and format validated"
                    elif 'DateFormats' in method_name:
                        expected_result = "Date parameter formats validated"
                    elif 'VersionFormats' in method_name:
                        expected_result = "Version parameter formats validated"
                    elif 'ClinicIdFormats' in method_name:
                        expected_result = "Clinic ID parameter formats validated"
                    elif 'QueryParameters' in method_name:
                        expected_result = "Query parameters validated successfully"
                    elif 'CurlCommandStructure' in method_name:
                        expected_result = "Curl command structure validated"
                    elif 'AuthenticationHeaders' in method_name:
                        expected_result = "Authentication headers handled correctly"
                    else:
                        expected_result = "Validation passed successfully"
                elif 'ShouldReturn' in method_name:
                    if 'InventoryProducts' in method_name:
                        expected_result = "200 OK with inventory products data"
                    elif 'LotNumbersData' in method_name:
                        expected_result = "200 OK with lot numbers data"
                    elif 'LotInventoryData' in method_name:
                        expected_result = "200 OK with lot inventory data"
                    elif 'ClinicData' in method_name:
                        expected_result = "200 OK with clinic data"
                    elif 'InsuranceData' in method_name:
                        expected_result = "200 OK with insurance data"
                    elif 'ProvidersData' in method_name:
                        expected_result = "200 OK with providers data"
                    elif 'ShotAdministratorsData' in method_name:
                        expected_result = "200 OK with shot administrators data"
                    elif 'UsersPartnerLevelData' in method_name:
                        expected_result = "200 OK with users partner level data"
                    elif 'LocationData' in method_name:
                        expected_result = "200 OK with location data"
                    elif 'CheckData' in method_name:
                        expected_result = "200 OK with check data response"
                    elif 'AppointmentData' in method_name:
                        expected_result = "200 OK with appointment data"
                    elif 'AppointmentId' in method_name:
                        expected_result = "200 OK with appointment ID returned"
                    else:
                        expected_result = "200 OK with data returned"
                elif 'ShouldHandle' in method_name:
                    if 'UniquePatientNames' in method_name:
                        expected_result = "200 OK with unique patient appointment created"
                    elif 'InvalidAppointmentId' in method_name:
                        expected_result = "400 Bad Request or appropriate error for invalid appointment ID"
                    else:
                        expected_result = "Proper handling of scenario"
                elif 'ShouldDemonstrate' in method_name:
                    if 'ResponseLogging' in method_name:
                        expected_result = "Response logging demonstrated successfully"
                    else:
                        expected_result = "Demonstration completed successfully"
                else:
                    expected_result = "Test execution completed successfully"
                
                # Extract endpoint from test name patterns
                if 'Inventory' in class_name:
                    endpoint = "GET /api/inventory"
                elif 'Appointment' in class_name:
                    if 'Create' in method_name:
                        endpoint = "POST /api/patients/appointment"
                    elif 'Sync' in method_name:
                        endpoint = "GET /api/patients/appointment/sync"
                    elif 'Checkout' in method_name:
                        endpoint = "PUT /api/patients/appointment/{id}/checkout"
                elif 'Clinic' in class_name:
                    endpoint = "GET /api/patients/clinic"
                elif 'Insurance' in class_name:
                    endpoint = "GET /api/patients/insurance"
                elif 'Staffer' in class_name:
                    endpoint = "GET /api/patients/staffer"
                elif 'Setup' in class_name:
                    endpoint = "GET /api/setup"
            
            test_results.append({
                'name': display_name,
                'full_name': test_name,
                'class': class_name,
                'result': outcome,
                'duration': duration_ms,
                'duration_ms': round(duration_ms, 2),
                'status_icon': '&#10004;' if outcome == 'Passed' else '&#10008;' if outcome == 'Failed' else '&#9193;',
                'description': description,
                'test_type': test_type,
                'endpoint': endpoint,
                'expected_result': expected_result,
                'actual_result': actual_result,
                'failure_reason': failure_reason
            })
        
        # Calculate success rate (excluding skipped tests from denominator)
        executed_tests = passed_tests + failed_tests
        success_rate = round((passed_tests / executed_tests) * 100, 1) if executed_tests > 0 else 0
        
        return {
            'total_tests': total_tests,
            'passed_tests': passed_tests,
            'failed_tests': failed_tests,
            'skipped_tests': skipped_tests,
            'success_rate': success_rate,
            'test_details': test_results
        }
        
    except Exception as e:
        safe_print(f"ERROR: Error parsing TRX file: {e}")
        sys.exit(1)

def generate_html_report(data, output_path):
    """Generate HTML report with actual results and failure reasons"""
    timestamp = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
    
    html_content = f"""<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>VaxCare API Test Report</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 1400px; margin: 0 auto; background: white; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #8B5CF6 0%, #A855F7 50%, #EC4899 100%); color: white; padding: 30px; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 2.5em; }}
        .header p {{ margin: 10px 0 0 0; opacity: 0.9; }}
        .content {{ padding: 30px; }}
        .stats {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; margin: 20px 0; }}
        .stat-card {{ background: #f8f9fa; padding: 20px; border-radius: 8px; text-align: center; border-left: 4px solid #28a745; }}
        .stat-card h3 {{ margin: 0 0 10px 0; color: #333; }}
        .stat-card .stat-number {{ font-size: 2em; font-weight: bold; color: #28a745; }}
        .stat-card .stat-label {{ color: #666; }}
        .passed .stat-number {{ color: #28a745; }}
        .failed .stat-number {{ color: #dc3545; }}
        .total .stat-number {{ color: #007bff; }}
        .success-rate .stat-number {{ color: #6f42c1; }}
        .test-table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        .test-table th, .test-table td {{ padding: 12px; text-align: left; border-bottom: 1px solid #ddd; }}
        .test-table th {{ background: #007bff; color: white; font-weight: bold; }}
        .test-table tbody tr:hover {{ background-color: #f5f5f5; }}
        .status-passed {{ color: #28a745; font-weight: bold; }}
        .status-failed {{ color: #dc3545; font-weight: bold; background-color: #f8d7da; padding: 5px; border-radius: 3px; }}
        .failed-test-row {{ background-color: #f8d7da; }}
        .actual-result {{ color: #dc3545; font-weight: bold; margin-top: 5px; }}
        .failure-reason {{ color: #dc3545; font-style: italic; margin-top: 3px; font-size: 0.9em; }}
        .duration {{ font-family: monospace; background: #f8f9fa; padding: 2px 6px; border-radius: 3px; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; }}
        .warning {{ background: #fff3cd; border: 1px solid #ffeaa7; color: #856404; padding: 10px; border-radius: 4px; margin: 10px 0; }}
        .test-info {{ margin-top: 10px; padding: 10px; background: #e9ecef; border-radius: 4px; font-size: 0.9em; }}
        .failure-info {{ margin-top: 10px; padding: 10px; background: #f8d7da; border: 1px solid #dc3545; border-radius: 4px; font-size: 0.9em; }}
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>💉 VaxCare API Test Report</h1>
            <p>Generated: {timestamp}</p>
        </div>
        
        <div class="stats">
            <div class="stat-card passed">
                <div class="stat-number">{data['passed_tests']}</div>
                <div class="stat-label">Passed</div>
            </div>
            <div class="stat-card failed">
                <div class="stat-number">{data['failed_tests']}</div>
                <div class="stat-label">Failed</div>
            </div>
            <div class="stat-card total">
                <div class="stat-number">{data['total_tests']}</div>
                <div class="stat-label">Total</div>
            </div>
            <div class="stat-card success-rate">
                <div class="stat-number">{data['success_rate']}%</div>
                <div class="stat-label">Success Rate</div>
            </div>
        </div>
        
        <table class="test-table">
            <thead>
                <tr>
                    <th>Status</th>
                    <th>Test Name</th>
                    <th>Class</th>
                    <th>Duration</th>
                </tr>
            </thead>
            <tbody>"""
    
    # Add test details to table
    for test in data['test_details']:
        status_class = f"status-{test['result'].lower()}" if test['result'] in ['Passed', 'Failed', 'Skipped'] else 'status-unknown'
        row_class = "failed-test-row" if test['result'] == 'Failed' else ""
        
        # Add test information if available
        test_info_html = ""
        if test.get('description') or test.get('endpoint') or test.get('expected_result'):
            test_info_html = f"""
                <div class="test-info">
                    {f"<div><strong>Description:</strong> {test['description']}</div>" if test.get('description') else ""}
                    {f"<div><strong>Endpoint:</strong> {test['endpoint']}</div>" if test.get('endpoint') else ""}
                    {f"<div><strong>Expected Result:</strong> {test['expected_result']}</div>" if test.get('expected_result') else ""}
                </div>"""
        
        # Add failure information for failed tests
        failure_info_html = ""
        if test['result'] == 'Failed' and (test.get('actual_result') or test.get('failure_reason')):
            failure_info_html = f"""
                <div class="failure-info">
                    {f"<div class='actual-result'><strong>Actual Result:</strong> {test['actual_result']}</div>" if test.get('actual_result') else ""}
                    {f"<div class='failure-reason'><strong>Failure Reason:</strong> {test['failure_reason']}</div>" if test.get('failure_reason') else ""}
                </div>"""
        
        html_content += f"""
                <tr class="{row_class}">
                    <td class="{status_class}">{test['status_icon']} {test['result']}</td>
                    <td>
                        <div><strong>{test['name']}</strong></div>
                        {test_info_html}
                        {failure_info_html}
                    </td>
                    <td>{test['class']}</td>
                    <td><span class="duration">{test['duration_ms']}ms</span></td>
                </tr>"""
    
    html_content += f"""
            </tbody>
        </table>
        
        <div class="footer">
            <p>Report generated by VaxCare API Test Suite | {timestamp}</p>
        </div>
    </div>
</body>
</html>"""
    
    # Write HTML content to file
    try:
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write(html_content)
        safe_print(f"SUCCESS: HTML report generated: {output_path}")
        return True
    except Exception as e:
        safe_print(f"ERROR: Error writing HTML file: {e}")
        return False

def main():
    parser = argparse.ArgumentParser(description='Generate enhanced HTML test report with actual results - Windows Compatible')
    parser.add_argument('--trx', default='TestResults/TestResults_2025-10-24_09-56-03.trx', help='TRX file path')
    parser.add_argument('--output', default='TestReports', help='Output directory')
    
    args = parser.parse_args()
    
    # Create output directory if it doesn't exist
    os.makedirs(args.output, exist_ok=True)
    
    timestamp = datetime.now().strftime('%Y-%m-%d_%H-%M-%S')
    html_report_path = os.path.join(args.output, f'EnhancedTestReport_WithActualResults_{timestamp}.html')
    
    safe_print("Generating enhanced HTML report with actual results...")
    
    # Check if TRX file exists
    if not os.path.exists(args.trx):
        safe_print(f"ERROR: TRX file not found: {args.trx}")
        sys.exit(1)
    
    # Parse TRX and extract data
    data = parse_trx_file(args.trx)
    
    # Print statistics
    safe_print("Test Statistics:")
    safe_print(f"   Total Tests: {data['total_tests']}")
    safe_print(f"   Passed: {data['passed_tests']}")
    safe_print(f"   Failed: {data['failed_tests']}")
    safe_print(f"   Skipped: {data['skipped_tests']}")
    safe_print(f"   Success Rate: {data['success_rate']}%")
    
    # Generate HTML report
    if generate_html_report(data, html_report_path):
        safe_print("SUCCESS: Enhanced HTML report with actual results generation completed!")
    else:
        sys.exit(1)

if __name__ == "__main__":
    main()
