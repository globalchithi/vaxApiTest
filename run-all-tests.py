#!/usr/bin/env python3
"""
Test Runner Wrapper
This script calls the main test runner in the TestRunner folder
"""

import sys
import subprocess
import os

def main():
    """Run the main test runner"""
    # Get the directory where this script is located
    script_dir = os.path.dirname(os.path.abspath(__file__))
    test_runner_script = os.path.join(script_dir, "TestRunner", "run-all-tests.py")
    
    # Check if the TestRunner script exists
    if not os.path.exists(test_runner_script):
        print("❌ TestRunner/run-all-tests.py not found!")
        print("Please ensure the TestRunner folder contains the test runner script.")
        sys.exit(1)
    
    # Pass all arguments to the TestRunner script
    cmd = [sys.executable, test_runner_script] + sys.argv[1:]
    
    try:
        # Run the TestRunner script with all arguments
        result = subprocess.run(cmd, check=False)
        sys.exit(result.returncode)
    except KeyboardInterrupt:
        print("\n🛑 Test execution interrupted by user")
        sys.exit(1)
    except Exception as e:
        print(f"❌ Error running test runner: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()