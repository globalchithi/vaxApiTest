#!/usr/bin/env python3
"""
HTML Report Opener Wrapper
This script calls the HTML report opener in the TestRunner folder
"""

import sys
import subprocess
import os

def main():
    """Run the HTML report opener"""
    # Get the directory where this script is located
    script_dir = os.path.dirname(os.path.abspath(__file__))
    report_opener_script = os.path.join(script_dir, "TestRunner", "open-html-report.py")
    
    # Check if the TestRunner script exists
    if not os.path.exists(report_opener_script):
        print("❌ TestRunner/open-html-report.py not found!")
        print("Please ensure the TestRunner folder contains the report opener script.")
        sys.exit(1)
    
    # Pass all arguments to the TestRunner script
    cmd = [sys.executable, report_opener_script] + sys.argv[1:]
    
    try:
        # Run the TestRunner script with all arguments
        result = subprocess.run(cmd, check=False)
        sys.exit(result.returncode)
    except KeyboardInterrupt:
        print("\n🛑 Report opening interrupted by user")
        sys.exit(1)
    except Exception as e:
        print(f"❌ Error opening HTML report: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()